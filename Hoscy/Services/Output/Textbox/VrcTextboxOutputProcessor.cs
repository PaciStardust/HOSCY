using System;
using System.Collections.Generic;
using System.Linq;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Osc.SendReceive;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Textbox;

[LoadIntoDiContainer(typeof(VrcTextboxOutputProcessor), Lifetime.Transient)]
public class VrcTextboxOutputProcessor(ILogger logger, ConfigModel config, IOscSendService sender) : IOutputProcessor
{
    #region Injected Services
    private readonly ILogger _logger = logger.ForContext<VrcTextboxOutputProcessor>();
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    #endregion

    #region Processor Variables
    private (string, OutputNotificationPriority)? _currentNotification = null;
    private readonly Queue<string> _currentMessages = [];
    private const int TIMEOUT_MINIMUM_MS = 1250;
    private bool _isClearPending = false;
    private DateTime _intendedTimeoutUntil = DateTime.MinValue;
    private bool _lastSetProcessingState = false;
    private DateTime _lastSentTypingIndicator = DateTime.MinValue;
    #endregion

    #region Events
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnShutdownCompleted = delegate { };
    #endregion

    #region Information
    public OutputProcessorInfo GetInfo()
        => _info;

    private readonly OutputProcessorInfo _info = new()
    {
        Name = "VRChat Textbox",
        Description = "Sends Output to the VRChat Textbox via OSC",
        Flags = OutputProcessorInfoFlags.SupportsMessages | OutputProcessorInfoFlags.SupportsNotifications | OutputProcessorInfoFlags.SupportsProcessingIndicator,
        ProcessorType = typeof(VrcTextboxOutputProcessor)
    };
    #endregion

    #region Processing Indicator
    /// <summary>
    /// Enables typing indicator for textbox
    /// Note: This only stays on for 5 seconds ingame
    /// </summary>
    public void SetProcessingIndicator(bool isProcessing)
    {
        if (!isProcessing && !_lastSetProcessingState) return;
        if (!CanSetProcessingIndicator()) return;

        _lastSentTypingIndicator = isProcessing ? DateTime.Now : DateTime.MinValue;
        _lastSetProcessingState = isProcessing;

        _sender.SendToDefaultSyncFireAndForget("/chatbox/typing", isProcessing ? 1 : 0); //todo: should this be fire and forget?
    }

    private bool CanSetProcessingIndicator()
    {
        return _lastSentTypingIndicator.AddSeconds(4) > DateTime.Now
            && _config.Textbox_Text_TypingIndicatorWhenSpeaking
            && (_config.Input_UseTextbox || _config.Textbox_Text_TypingIndicatorWhenDisabled); //todo: UseTextbox check needs a redo
    }
    #endregion

    #region Input Processing
    public void ProcessMessage(string contents)  
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
					if (currentSegmentStart == -1) {
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

    public void Activate()
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public Exception? GetFaultIfExists()
    {
        throw new NotImplementedException();
    }

    public StartStopStatus GetStatus()
    {
        throw new NotImplementedException();
    }

    public bool IsRunning()
    {
        throw new NotImplementedException();
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public void ProcessNotification(string contents, OutputNotificationPriority priority)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }
}