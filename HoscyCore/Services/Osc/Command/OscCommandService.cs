using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Osc.Command;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)] //todo: [FIX?] Does this notify?
public class OscCommandService(ILogger logger, IOscCommandParser parser, IOscSendService sender)
    : StartStopServiceBase(logger.ForContext<OscCommandService>()), IOscCommandService
{
    private readonly IOscCommandParser _parser = parser;
    private readonly IOscSendService _sender = sender;
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource? _cts = null;

    private const int OSC_COMMAND_MAX_UNINTERRUPTED_WAIT = 50;

    #region Funtionality
    public string CommandIdentifier
        => OscCommandParser.OSC_COMMAND_IDENTIFIER;

    public bool DetectCommand(string commandString)
    {
        if (!_parser.DetectCommandPrefix(commandString)) return false;
        _logger.Verbose("Detected OSC command identifier in string \"{commandString}\"", commandString);
        return true;
    }

    public Res<OscCommandState> HandleCommand(string commandString)
    {
        if (_cts is null || _cts.IsCancellationRequested)
            return ResC.TFail<OscCommandState>(ResMsg.Err($"Unable to handle OSC command \"{commandString}\", service is shut down"));

        _logger.Debug("Attempting to parse command string \"{commandString}\"", commandString);
        var parseResult = _parser.Parse(commandString);
        if (!parseResult.IsOk) 
            return ResC.TFail<OscCommandState>(parseResult.Msg);

        if (parseResult.Value.Length == 0)
            return ResC.TFailLog<OscCommandState>($"Failed to find any command messages to execute in string \"{commandString}\"", _logger, lvl: ResMsgLvl.Warning);

        var threadId = "ST-" + Guid.NewGuid().ToString().Split('-')[0];
        _logger.Debug("Parsed {commandMessageCount} OSC command infos from string \"{commandString}\", executing as id {threadId}",
            parseResult.Value.Length, commandString, threadId);
        var task = Task.Run(() => ExecuteOscCommands(threadId, parseResult.Value));

        PerformTaskCleanup();
        _runningTasks.Add(task);
        return ResC.TOk(OscCommandState.Success);
    }

    public Res<OscCommandState> DetectAndHandleCommand(string commandString)
    {
        if (!DetectCommand(commandString))
            return ResC.TOk(OscCommandState.NotCommand);

        return HandleCommand(commandString);
    }

    /// <summary>
    /// Runs all osc commands asyncronously
    /// </summary>
    /// <param name="threadId">identifier for thread</param>
    /// <param name="commandPackets">packets to execute with wait</param>
    private Task ExecuteOscCommands(string taskId, OscCommandInfo[] commandInfos)
    {
        int cmdCount = commandInfos.Length;
        _logger.Debug("{taskId}: Started OSC subcommand execution task of length {cmdCount}", taskId, cmdCount);

        for (int i = 0; i < cmdCount; i++)
        {
            if (_cts is null || _cts.IsCancellationRequested)
            {
                _logger.Verbose("{taskId}: Cancelled early during step {step}", taskId, i + 1);
                return Task.CompletedTask;
            }

            var cmdInfo = commandInfos[i];
            _logger.Verbose("{taskId}: Step {step}/{cmdCount} sending {cmdInfo}", taskId, i + 1, cmdCount, cmdInfo.ToString());
            var sendResult = _sender.SendSync(cmdInfo.Ip ?? _sender.GetDefaultIp(), cmdInfo.Port ?? _sender.GetDefaultPort(), cmdInfo.Address, cmdInfo.Arguments);
            if (!sendResult.IsOk)
            {
                _logger.Warning("{taskId}: Step {step}/{cmdCount} with info {cmdInfo} failed to send", taskId, i + 1, cmdCount, cmdInfo.ToString());
                return Task.CompletedTask;
            }

            if (i != cmdCount - 1 && cmdInfo.Wait.HasValue && cmdInfo.Wait.Value > 0)
            {
                _logger.Verbose("{taskId}: Step {step}/{cmdCount} waiting for {timeout}ms after execution", taskId, i + 1, cmdCount, cmdInfo.Wait);
                var timeToWait = cmdInfo.Wait.Value;
                while (timeToWait > 0) //this loop ensures that we can exit within 50ms of the token being cancelled
                {
                    var waitCycle = Math.Min(timeToWait, OSC_COMMAND_MAX_UNINTERRUPTED_WAIT);
                    Task.Delay(waitCycle).GetAwaiter().GetResult();
                    if (_cts.IsCancellationRequested) return Task.CompletedTask;
                    timeToWait -= waitCycle;
                }
            }

        }
        _logger.Debug("{taskId}: Finished processing {cmdCount} subcommands", taskId, cmdCount);
        return Task.CompletedTask;
    }
    #endregion

    #region Task Management
    /// <summary>
    /// Checks list of tasks for any that are completed and removes them
    /// </summary>
    /// <returns>Currently running task count</returns>
    private int PerformTaskCleanup()
    {
        var currentCount = _runningTasks.Count;
        _logger.Verbose("Performing Task Cleanup, currently {currentCount} in list", currentCount);
        for (var i = currentCount - 1; i > -1; i--)
        {
            if (_runningTasks[i].IsCompleted)
            {
                _runningTasks.RemoveAt(i);
            }
        }
        _logger.Verbose("Performed Task Cleanup, currently {oldCount} => {currentCount} in list", currentCount, _runningTasks.Count);
        return _runningTasks.Count;
    }
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    {
        if (_cts is null)
        {
            _cts = new();
            _logger.Debug("Service started");    
        } 
        else
        {
            _logger.Debug("Service not started, it is already running");    
        }

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => false;

    protected override bool IsStarted()
        => _cts is not null && !_cts.IsCancellationRequested;
    protected override bool IsProcessing()
        => IsStarted();

    protected override Res StopForService()
    {
        _logger.Verbose("Ensuring all tasks ended");

        try
        {
            _cts?.Cancel();

            var remain = PerformTaskCleanup();
            if (remain > 0)
            {
                _logger.Verbose("{remain} tasks are still running, waiting a timer period to allow them to safely stop", remain);
                Thread.Sleep(OSC_COMMAND_MAX_UNINTERRUPTED_WAIT + 1); //This should ensure all tasks to have a chance of exiting unless stuck
                remain = PerformTaskCleanup();
                if (remain > 0)
                {
                    _logger.Warning("{remain} tasks refused to stop in expected duration, forcing shutdown", remain);
                    _runningTasks.Clear();
                }
            }
        } 
        catch (Exception ex)
        {
            return ResC.FailLog("Failed stopping tasks", _logger, ex);
        }

        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _runningTasks.Clear();
        _cts?.Dispose();
        _cts = null;
    }
    #endregion
}