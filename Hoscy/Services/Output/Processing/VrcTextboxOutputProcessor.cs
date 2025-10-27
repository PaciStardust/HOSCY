using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Osc.SendReceive;
using Hoscy.Services.Output.Core;
using Hoscy.Utility;
using Serilog;

namespace Hoscy.Services.Output.Processing;

[LoadIntoDiContainer(typeof(VrcTextboxOutputProcessor), Lifetime.Transient)]
public class VrcTextboxOutputProcessor(ILogger logger, ConfigModel config, IOscSendService sender) : OutputProcessorBase //todo: is no event ever called?
{
    #region Injected Services
    private readonly ILogger _logger = logger.ForContext<VrcTextboxOutputProcessor>();
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    #endregion

    #region Processor Variables
    //Queue
    private (string, OutputNotificationPriority)? _currentNotification = null;
    private readonly Queue<string> _currentMessages = [];

    //Processing Indicator
    private bool _lastSetProcessingState = false;
    private DateTime _lastSentTypingIndicator = DateTime.MinValue;

    //Send Loop
    private const int TIMEOUT_MINIMUM_MS = 1250;
    private const int TIMEOUT_WAIT_MS = 50;
    private CancellationTokenSource? _cts = null;
    private Task? _workerTask = null;
    private bool _isClearPending = false;
    private DateTime _intendedTimeoutUntil = DateTime.MinValue;
    private DateTime _minimumTimeoutUntil = DateTime.MinValue;
    private OutputNotificationPriority? _lastSentNotificationPriority = null;
    #endregion

    #region Information
    public override OutputProcessorInfo GetIdentifier()
        => _info;

    private readonly OutputProcessorInfo _info = new()
    {
        Name = "VRChat Textbox",
        Description = "Sends Output to the VRChat Textbox via OSC",
        Flags = OutputProcessorInfoFlags.SupportsMessages | OutputProcessorInfoFlags.SupportsNotifications | OutputProcessorInfoFlags.SupportsProcessingIndicator,
        ProcessorType = typeof(VrcTextboxOutputProcessor)
    };
    #endregion

    #region Logic Loop
    private async Task ProcessingLoop()
    {
        _logger.Information("Started textbox message processing loop");
        while (_cts is not null && !_cts.IsCancellationRequested)
        {
            await ProcessingLoopLogic();
        }
        _logger.Information("Stopped textbox message processing loop");
    }

    private async Task ProcessingLoopLogic()
    {
        var now = DateTime.Now;

        //If we have not reached the minimum timeout yet, wait a bit and exit
        if (_minimumTimeoutUntil > now)
        {
            await Task.Delay(TIMEOUT_WAIT_MS);
            return;
        }

        string? textToSend = null;
        var playTextboxSound = false;

        //Messages have priority
        //Only sends once timeout has passed OR the last sent was a notification and skip is enabled
        if (_currentMessages.Count > 0)
        {
            var timeoutBypass = _lastSentNotificationPriority is not null && _config.Textbox_Notification_SkipWhenMessageAvailable;
            
            if (now >= _intendedTimeoutUntil || timeoutBypass)
            {
                if (timeoutBypass)
                {
                    _logger.Debug("Notification timeout was shortened due to incoming message");
                }

                textToSend = _currentMessages.Dequeue();
                playTextboxSound = _config.Textbox_Sound_OnMessage;
                _lastSentNotificationPriority = null;
                _isClearPending = _config.Textbox_Timeout_AutomaticallyClearMessage;
            }
        }

        //Notifications only get handled when we do not have messages
        //Only sends once timeout has passed OR the last sent message was a notification and of the same type or higher priority
        else if (_currentNotification is not null)
        {
            var timeoutBypass = _lastSentNotificationPriority is not null
                && _config.Textbox_Notification_UsePrioritySystem
                && _currentNotification.Value.Item2 >= _lastSentNotificationPriority;

            if (now >= _intendedTimeoutUntil || timeoutBypass)
            {
                if (timeoutBypass)
                {
                    _logger.Debug("Notification timeout was shortened due to last priority {lastPriority} being lower or equal to current {currentPriority}", _lastSentNotificationPriority, _currentNotification.Value.Item2);
                }

                textToSend = _currentNotification.Value.Item1;
                var prio = _currentNotification.Value.Item2;
                ClearNotification();
                playTextboxSound = _config.Textbox_Sound_OnNotification;
                _lastSentNotificationPriority = prio;
                _isClearPending = _config.Textbox_Timeout_AutomaticallyClearNotification;
            }
        }

        //Handles clearing if nothing else is there to be displayed
        else if (_isClearPending && now >= _intendedTimeoutUntil)
        {
            //Early timeout
            SendMessage(string.Empty, false);
            _isClearPending = false;
            _minimumTimeoutUntil = now.AddMilliseconds(TIMEOUT_MINIMUM_MS);
            _intendedTimeoutUntil = DateTime.MinValue;
        }

        //Send if we have anything
        if (!string.IsNullOrWhiteSpace(textToSend))
        {
            SendMessage(textToSend, playTextboxSound);
            var msgTimeout = GetMessageTimeout(textToSend);
            _intendedTimeoutUntil = DateTime.Now.AddMilliseconds(msgTimeout);

            if (_lastSentNotificationPriority is not null)
                _logger.Information("Sent notification with timeout {threadSleep}-{msgTimeout}: {textToSend}", TIMEOUT_MINIMUM_MS, msgTimeout, textToSend);
            else
                _logger.Information("Sent message with timeout {threadSleep}-{msgTimeout}: {textToSend}", TIMEOUT_MINIMUM_MS, msgTimeout, textToSend);

            _minimumTimeoutUntil = now.AddMilliseconds(TIMEOUT_MINIMUM_MS);
            _intendedTimeoutUntil = now.AddMilliseconds(msgTimeout);
        }
        
        await Task.Delay(TIMEOUT_WAIT_MS);
    }

    private const int VRC_TEXTBOX_LIMIT = 140;
    private void SendMessage(string message, bool playSound)
    {
        if (message.Length > VRC_TEXTBOX_LIMIT) //Clamp for VRC
            message = message[..VRC_TEXTBOX_LIMIT];

        _sender.SendToDefaultSyncFireAndForget(_config.Osc_Address_Game_Textbox, message, playSound);
    }

    private double GetMessageTimeout(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return TIMEOUT_MINIMUM_MS; //to avoid hitting ratelimit

        if (!_config.Textbox_Timeout_UseDynamic)
            return _config.Textbox_Timeout_StaticMs;

        var timeout = (int)(Math.Ceiling(message.Length / 20f) * _config.Textbox_Timeout_DynamicPer20CharactersDisplayedMs);
        return Math.Max(timeout, _config.Textbox_Timeout_DynamicMinimumMs);
    }
    #endregion

    #region Processing Indicator
    /// <summary>
    /// Enables typing indicator for textbox
    /// Note: This only stays on for 5 seconds ingame
    /// </summary>
    public override void SetProcessingIndicator(bool isProcessing)
    {
        if (!isProcessing && !_lastSetProcessingState) return;
        if (!CanSetProcessingIndicator()) return;

        _lastSentTypingIndicator = isProcessing ? DateTime.Now : DateTime.MinValue;
        _lastSetProcessingState = isProcessing;

        _sender.SendToDefaultSyncFireAndForget("/chatbox/typing", isProcessing ? 1 : 0);
    }

    private bool CanSetProcessingIndicator()
    {
        return _lastSentTypingIndicator.AddSeconds(4) > DateTime.Now
            && _config.Textbox_Text_TypingIndicatorWhenSpeaking
            && (_config.Input_UseTextbox || _config.Textbox_Text_TypingIndicatorWhenDisabled); //todo: UseTextbox check needs a redo
    }
    #endregion

    #region Input Processing
    public override void ProcessNotification(string contents, OutputNotificationPriority priority)
    {
        if (string.IsNullOrWhiteSpace(contents))
        {
            if (_currentNotification.HasValue && _currentNotification.Value.Item2 == priority)
                ClearNotification();
            return;
        }

        if (_config.Textbox_Notification_UsePrioritySystem && _currentNotification.HasValue && priority < _currentNotification.Value.Item2)
        {
            _logger.Debug("Did not override notification with contents \"{notificationContents}\" and priority {notificationPriority} => priority lower than current {currentPriority}",
                contents, priority, _currentNotification.Value.Item2);
            return;
        }

        var indLen = _config.NotificationIndicatorLength();
        if (contents.Length > _config.Textbox_Text_MaxDisplayedCharacters - indLen)
        {
            contents = contents[..(_config.Textbox_Text_MaxDisplayedCharacters - 1)] + "-";
        }
        contents = $"{_config.Textbox_Notification_IndicatorTextStart}{contents}{_config.Textbox_Notification_IndicatorTextEnd}";

        _logger.Information("Setting notification to \"{contents}\" with priority {priority}", contents, priority);
        _currentNotification = (contents, priority);
    }

    public override void ProcessMessage(string contents)  
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        foreach (var message in SplitMessageIntoSegments(contents))
        {
            _logger.Debug("Added to MessageQueue (Position:{queuePosition}, Length:{messageLength}) Message:\"{messageContents}\"", _currentMessages.Count, message.Length, message);
            _currentMessages.Enqueue(message);
        }
    }   

    private const string SPLIT_LONG_WORD_CUTOFF = "-";
    private const string SPLIT_HAS_PREVIOUS_INDICATOR = "... ";
    private const string SPLIT_HAS_AFTER_INDICATOR = " ...";
    /// <summary>
    /// Splitting message into displayable segments
    /// </summary>
    /// <returns>Split message</returns>
    private List<string> SplitMessageIntoSegments(string message)
    {
        var segments = new List<(int, int)>(); //Should be pairs of values => 1st is start index, 2nd is length
        var currentSegmentStart = -1;
        var currentWordStart = -1;
        var currentSegmentPotentialEnd = -1;
        var maxLength = _config.Textbox_Text_MaxDisplayedCharacters;
        for (var charIndex = 0; charIndex < message.Length; charIndex++)
        {
            var isWordSeparator = message[charIndex] == ' ' || message[charIndex] == '\r' || message[charIndex] == '\n' || message[charIndex] == '\t';
            if (currentWordStart == -1) // We were not in a word
            {
                if (!isWordSeparator) //We are now in a word
                {
                    if (currentSegmentStart == -1)
                    {
                        currentSegmentStart = charIndex;
                    }
                    currentWordStart = charIndex;
                }
                continue;
            }

            if (!isWordSeparator) continue; //Do not do any logic if we are still in a word

            var expectedLength = charIndex - currentSegmentStart;
            if (expectedLength <= maxLength) //Completed word is within limits
            {
                currentWordStart = -1;
                currentSegmentPotentialEnd = charIndex;
                continue;
            }

            if (currentSegmentPotentialEnd != -1) //Not the first word in segment => At least 1 word already fit in
            {
                segments.Add((currentSegmentStart, currentSegmentPotentialEnd - currentSegmentStart));
                currentSegmentPotentialEnd = -1;
                currentSegmentStart = currentWordStart;
            }
            else //This word is the first word and too long for bounds
            {
                segments.Add((currentSegmentStart, charIndex - currentSegmentStart));
                currentSegmentStart = -1;
            }
            currentWordStart = -1;
        }
        if (currentSegmentStart != -1) //Handles loop ending on a valid character
        {
            segments.Add((currentSegmentStart, message.Length - currentSegmentStart));
        }

        var messageSegments = new List<string>();
        if (segments.Count == 0) return messageSegments;

        for (var i = 0; i < segments.Count; i++)
        {
            var length = segments[i].Item2;
            var aboveLimits = false;
            if (length > maxLength)
            {
                length = maxLength - 1;
                aboveLimits = true;
            }

            var text = $"{(i > 0 ? SPLIT_HAS_PREVIOUS_INDICATOR : string.Empty)}{message.Substring(segments[i].Item1, length)}{(aboveLimits ? SPLIT_LONG_WORD_CUTOFF : string.Empty)}{(i + 2 < segments.Count ? SPLIT_HAS_AFTER_INDICATOR : string.Empty)}";
            messageSegments.Add(text);
        }
        return messageSegments;
    }
    #endregion

    #region Input Cleaning
    public override void Clear()
    {
        _logger.Information("Clearing message queue");
        _currentMessages.Clear();
        ClearNotification();
        _isClearPending = true;
        _intendedTimeoutUntil = DateTime.MinValue; //Min timeout never gets cleared because of VRC rate limits
    }

    private void ClearNotification()
    {
        _lastSentNotificationPriority = null;
        _currentNotification = null;
    }
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Information("Starting up message processing loop");
        if (IsRunning())
        {
            _logger.Information("Skipped starting loop, still running");
            return;
        }
        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(ProcessingLoop);
        _logger.Information("Loop has been started");
    }

    protected override void StopInternal()
    {
        _logger.Information("Stopping loop...");
        _cts?.Cancel();
        var ex = LaunchUtils.SafelyWaitForTaskWithTimeoutAndLogException(_workerTask, TIMEOUT_WAIT_MS * 2, new StartStopServiceException("Message handling loop failed to stop within time limit"));
        if (ex is not null)
        {
            _logger.Error(ex, "Caught exception while stopping loop");
        }
        _logger.Debug("Cleanup of internals...");
        _cts?.Dispose();
        _cts = null;
        _workerTask = null;
        _logger.Information("Loop stopped");
    }

    public override bool IsRunning()
    {
        return _cts is not null || _workerTask is not null;
    }

    public override void Restart()
    {
        RestartSimple(_info.Name, _logger);
    }
    #endregion
}