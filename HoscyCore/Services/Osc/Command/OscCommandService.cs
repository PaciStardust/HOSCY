using System.Globalization;
using System.Text.RegularExpressions;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Osc.Query;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Osc.Command;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)] //todo: [FIX?] Does this notify?
public class OscCommandService(ILogger logger, OscQueryHostRegistry hostRegistry, IOscSendService sender)
    : StartStopServiceBase(logger.ForContext<OscCommandService>()), IOscCommandService
{
    private readonly OscQueryHostRegistry _hostRegistry = hostRegistry;
    private readonly IOscSendService _sender = sender;
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource? _cts = null;

    private static readonly Regex _oscCommandIdentifier = new(@"\[ *(?<address>(?:\/[^\ #\*,\/?\[\]\{\}]+)+)(?<values>(?: +\[(?:[fF]\]-?[0-9]+(?:\.[0-9]+)?|[iI]\]\-?[0-9]+|[sS]\]""[^""]*""|[bB]\](?:[tT](?:rue)?|[fF](?:alse)?|[0-1])))+)(?: +(?<ip>(?:(?:25[0-5]|(?:2[0-4]|1\d|[1-9]|)\d)\.?\b){4})?:(?<port>[0-9]{1,5})?)?(?: +""(?<target>[^""]*)"")?(?: +[wW](?<wait>[0-9]+))? *\]",RegexOptions.CultureInvariant);
    private static readonly Regex _oscParameterExtractor = new(@" +\[(?<type>[iIfFbBsS])\](?:""(?<value>[^""]*)""|(?<value>[a-zA-Z]+|[0-9\.\-]*))", RegexOptions.CultureInvariant);

    private const string OSC_COMMAND_IDENTIFIER = "[OSC]";
    private const int OSC_COMMAND_MAX_UNINTERRUPTED_WAIT = 50;

    #region Funtionality
    public string CommandIdentifier
        => OSC_COMMAND_IDENTIFIER;

    public bool DetectCommand(string commandString)
    {
        if (!commandString.StartsWith(OSC_COMMAND_IDENTIFIER, StringComparison.OrdinalIgnoreCase))
            return false;

        _logger.Verbose("Detected OSC command identifier in string \"{commandString}\"", commandString);
        return true;
    }

    public Res<OscCommandState> HandleCommand(string commandString)
    {
        if (_cts is null || _cts.IsCancellationRequested)
            return ResC.TFail<OscCommandState>(ResMsg.Err($"Unable to handle OSC command \"{commandString}\", service is shut down"));

        _logger.Debug("Attempting to parse command string \"{commandString}\"", commandString);
        var commandMatches = _oscCommandIdentifier.Matches(commandString);
        if (commandMatches is null || commandMatches.Count == 0) //todo: [FIX] Validation should also take into account count of passed commands somehow?
            return ResC.TFailLog<OscCommandState>("Failed parsing OSC command \"{commandString}\", it did not match the filter", _logger, lvl: ResMsgLvl.Warning);

        _logger.Verbose("{commandMatchCount} OSC command matches found in string \"{commandString}\", parsing individual commands",
            commandMatches.Count, commandString);
        var commandInfos = new List<OscCommandInfo>();
        foreach (Match commandMatch in commandMatches)
        {
            var stringParseOutput = ParseOscCommandString(commandMatch);
            if (!stringParseOutput.IsOk)
                return ResC.TFail<OscCommandState>(stringParseOutput.Msg);

            commandInfos.Add(stringParseOutput.Value);
        }

        if (commandInfos.Count == 0)
            return ResC.TFailLog<OscCommandState>($"Failed to find any command messages to execute in string \"{commandString}\"", _logger, lvl: ResMsgLvl.Warning);

        var threadId = "ST-" + Guid.NewGuid().ToString().Split('-')[0];
        _logger.Debug("Parsed {commandMessageCount} OSC command infos from string \"{commandString}\", executing as id {threadId}",
            commandInfos.Count, commandString, threadId);
        var task = Task.Run(() => ExecuteOscCommands(threadId, commandInfos));

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
    /// Turns a command string into a message and wait
    /// </summary>
    private Res<OscCommandInfo> ParseOscCommandString(Match commandMatch)
    {
        _logger.Verbose("Attempting to parse OSC subcommand \"{subcommandString}\"", commandMatch.Value);

        string addressText = commandMatch.Groups["address"].Value;
        string valuesText = commandMatch.Groups["values"].Value;
        string? targetText = commandMatch.Groups["target"].Value.Length == 0 ? null : commandMatch.Groups["target"].Value;
        string? ipText = commandMatch.Groups["ip"].Value.Length == 0 ? null : commandMatch.Groups["ip"].Value;
        string? portText = commandMatch.Groups["port"].Value.Length == 0 ? null : commandMatch.Groups["port"].Value;
        string? waitText = commandMatch.Groups["wait"].Value.Length == 0 ? null : commandMatch.Groups["wait"].Value;

        int? parsedWait;
        if (waitText is null)
        {
            parsedWait = null;
        }
        else
        {
            if (!int.TryParse(waitText, out var parsedWaitTmp))
            {
                var msg = $"Failed parsing OSC subcommand \"{commandMatch.Value}\", unable to parse wait \"{waitText}\"";
                return ResC.TFailLog<OscCommandInfo>(msg, _logger, lvl: ResMsgLvl.Warning);
            }
            parsedWait = parsedWaitTmp;
        }

        ushort? parsedPort;        
        if (portText is not null)
        {
            if (!ushort.TryParse(portText, out var parsedPortTmp))
            {
                var msg = $"Failed parsing OSC subcommand \"{commandMatch.Value}\", unable to parse port {portText}";
                return ResC.TFailLog<OscCommandInfo>(msg, _logger, lvl: ResMsgLvl.Warning);
            }
            parsedPort = parsedPortTmp;
        } 
        else
        {
            parsedPort = null;
        }

        if (targetText is not null && (ipText is null || portText is null))
        {
            var target = _hostRegistry.GetServiceAddressByName(targetText);
            if (!target.IsOk)
            {
                var msg = $"Failed parsing OSC subcommand \"{commandMatch.Value}\", specified target \"{targetText}\" not found";
                return ResC.TFailLog<OscCommandInfo>(msg, _logger, lvl: ResMsgLvl.Warning);
            }
            ipText ??= target.Value.Ip;
            parsedPort ??= target.Value.Port.ConvertToUshort();
        }

        var parsedVariables = ParseOscVariables(valuesText);
        if (!parsedVariables.IsOk)
        {
            return ResC.TFail<OscCommandInfo>(parsedVariables.Msg);
        }

        var info = new OscCommandInfo()
        {
            Address = addressText,
            Arguments = parsedVariables.Value.ToArray(),
            Ip = ipText,
            Port = parsedPort,
            Wait = parsedWait
        };
        
        _logger.Verbose("Parsed OSC subcommand \"{subcommandString}\"", commandMatch.Value);
        return ResC.TOk(info);
    }

    /// <summary>
    /// Parses osc variables from string
    /// </summary>
    private Res<List<object>> ParseOscVariables(string valuesText)
    {
        _logger.Verbose("Parsing OSC variables \"{valuesText}\"", valuesText);
        var variableMatches = _oscParameterExtractor.Matches(valuesText);
        var parsedVariables = new List<object>();

        foreach (Match variableMatch in variableMatches)
        {
            var type = variableMatch.Groups["type"].Value;
            var value = variableMatch.Groups["value"].Value;

            if (string.IsNullOrWhiteSpace(type))
            {
                var msg = $"Failed Parsing OSC variable in \"{valuesText}\" => type missing for value \"{value}\"";
                return ResC.TFailLog<List<object>>(msg, _logger, lvl: ResMsgLvl.Warning);
            }

            switch (type.ToLower())
            {
                case "s":
                    parsedVariables.Add(value);
                    continue;

                case "b":
                    parsedVariables.Add(value.StartsWith("t", StringComparison.OrdinalIgnoreCase) || value == "1");
                    continue;

                case "f":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat))
                        parsedVariables.Add(parsedFloat);
                    else
                        return InvalidTypeValueResult(valuesText, type, value);
                    continue;

                case "i":
                    if (int.TryParse(value, out var parsedInt))
                        parsedVariables.Add(parsedInt);
                    else
                        return InvalidTypeValueResult(valuesText, type, value);
                    continue;

                default:
                    return InvalidTypeValueResult(valuesText, type, value);
            }
        }

        _logger.Verbose("Parsed {parsedCount}/{totalCount} OSC variables from \"{valuesText}\"",
            parsedVariables.Count, variableMatches.Count, valuesText);
        return ResC.TOk(parsedVariables);
    }

    private Res<List<object>> InvalidTypeValueResult(string values, string type, string value)
    {
        var msg = $"Failed Parsing OSC variable in \"{values}\" => type \"{type}\" for value \"{value}\"";
        return ResC.TFailLog<List<object>>(msg, _logger, lvl: ResMsgLvl.Warning);
    }

    /// <summary>
    /// Runs all osc commands asyncronously
    /// </summary>
    /// <param name="threadId">identifier for thread</param>
    /// <param name="commandPackets">packets to execute with wait</param>
    private Task ExecuteOscCommands(string taskId, List<OscCommandInfo> commandInfos)
    {
        int cmdCount = commandInfos.Count;
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