using System.Globalization;
using System.Text.RegularExpressions;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)]
public partial class OscCommandService(ILogger logger, OscQueryHostRegistry hostRegistry, IOscSendService sender) : StartStopServiceBase, IOscCommandService
{
    private readonly ILogger _logger = logger.ForContext<OscCommandService>();
    private readonly OscQueryHostRegistry _hostRegistry = hostRegistry;
    private readonly IOscSendService _sender = sender;
    private readonly List<Task> _runningTasks = [];
    private readonly CancellationTokenSource _cts = new();

    private static readonly Regex _oscCommandIdentifier = new(@"\[ *(?<address>(?:\/[^\ #\*,\/?\[\]\{\}]+)+)(?<values>(?: +\[(?:[fF]\]-?[0-9]+(?:\.[0-9]+)?|[iI]\]\-?[0-9]+|[sS]\]""[^""]*""|[bB]\](?:[tT]rue|[fF]alse)))+)(?: +(?:(?<ip>(?:(?:25[0-5]|(?:2[0-4]|1\d|[1-9]|)\d)\.?\b){4}):(?<port>[0-9]{1,5})|""(?<target>[^""]*)""))?(?: +[wW](?<wait>[0-9]+))? *\]",RegexOptions.CultureInvariant);
    private static readonly Regex _oscParameterExtractor = new(@" +\[(?<type>[iIfFbBsS])\](?:""(?<value>[^""]*)""|(?<value>[a-zA-Z]+|[0-9\.\-]*))", RegexOptions.CultureInvariant);

    private const string OSC_COMMAND_IDENTIFIER = "[OSC]";
    private const int OSC_COMMAND_MAX_UNINTERRUPTED_WAIT = 50;

    #region Funtionality
    public string GetCommandIdentifier()
        => OSC_COMMAND_IDENTIFIER;

    public bool DetectCommand(string commandString)
    {
        if (!commandString.StartsWith(OSC_COMMAND_IDENTIFIER, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        _logger.Verbose("Detected OSC command identifier in string \"{commandString}\"", commandString);
        return true;
    }

    public OscCommandState HandleCommand(string commandString)
    {
        if (_cts.IsCancellationRequested) return OscCommandState.Shutdown;

        _logger.Debug("Attempting to parse command string \"{commandString}\"", commandString);
        var commandMatches = _oscCommandIdentifier.Matches(commandString);
        if (commandMatches is null || commandMatches.Count == 0)
        {
            _logger.Warning("Failed parsing OSC command, it did not match the filter");
            return OscCommandState.Malformed;
        }

        _logger.Verbose("{commandMatchCount} OSC command matches found in string \"{commandString}\", parsing individual commands", commandMatches.Count, commandString);
        var commandInfos = new List<OscCommandInfo>();
        foreach (Match commandMatch in commandMatches)
        {
            var output = ParseOscCommandString(commandMatch);
            if (output is null)
                return OscCommandState.Malformed;

            commandInfos.Add(output);
        }

        if (commandInfos.Count == 0)
        {
            _logger.Warning("Failed to find any command messages to execute");
            return OscCommandState.Malformed;
        }

        var threadId = "ST-" + Guid.NewGuid().ToString().Split('-')[0];
        _logger.Debug("Parsed {commandMessageCount} OSC command infos from string \"{commandString}\", executing as id {threadId}",
            commandInfos.Count, commandString, threadId);
        var task = Task.Run(() => ExecuteOscCommands(threadId, commandInfos));
        PerformTaskCleanup();
        _runningTasks.Add(task);
        return OscCommandState.Success;
    }

    public OscCommandState DetectAndHandleCommand(string commandString)
    {
        if (_cts.IsCancellationRequested) return OscCommandState.Shutdown;

        if (!DetectCommand(commandString)) return OscCommandState.NotCommand;

        return HandleCommand(commandString);
    }

    /// <summary>
    /// Turns a command string into a message and wait
    /// </summary>
    private OscCommandInfo? ParseOscCommandString(Match commandMatch)
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
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", unable to parse wait \"{wait}\"", commandMatch.Value, waitText);
                return null;
            }
            parsedWait = parsedWaitTmp;
        }

        ushort? parsedPort;
        if (targetText is not null)
        {
            var target = _hostRegistry.GetServiceAddressByName(targetText);
            if (!target.HasValue)
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", specified target \"{target}\" not found", commandMatch.Value, targetText);
                return null;
            }
            ipText = target.Value.Ip;
            parsedPort = target.Value.Port.ConvertToUshort();
        }
        else if (portText is not null)
        {
            if (!ushort.TryParse(portText, out var parsedPortTmp))
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", unable to parse port {port}", commandMatch.Value, portText);
                return null;
            }
            parsedPort = parsedPortTmp;
        }
        else
        {
            parsedPort = null;
        }

        var info = new OscCommandInfo()
        {
            Address = addressText,
            Arguments = ParseOscVariables(valuesText).ToArray(),
            Ip = ipText,
            Port = parsedPort,
            Wait = parsedWait
        };
        _logger.Verbose("Parsed OSC subcommand \"{subcommandString}\"", commandMatch.Value);
        return info;
    }

    /// <summary>
    /// Parses osc variables from string
    /// </summary>
    private List<object> ParseOscVariables(string valuesText)
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
                _logger.Warning("Failed Parsing OSC variable in \"{valuesText}\" => type missing for value \"{valueText}\"", valuesText, value);
                continue;
            }

            switch (type.ToLower())
            {
                case "s":
                    parsedVariables.Add(value);
                    continue;

                case "b":
                    parsedVariables.Add(value.Equals("true", StringComparison.OrdinalIgnoreCase));
                    continue;

                case "f":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat))
                        parsedVariables.Add(parsedFloat);
                    else
                        _logger.Warning("Failed Parsing OSC variable in \"{valuesText}\" => type \"{typeText}\" for value \"{valueText}\"", valuesText, type, value);
                    continue;

                case "i":
                    if (int.TryParse(value, out var parsedInt))
                        parsedVariables.Add(parsedInt);
                    else
                        _logger.Warning("Failed Parsing OSC variable in \"{valuesText}\" => type \"{typeText}\" for value \"{valueText}\"", valuesText, type, value);
                    continue;

                default:
                    _logger.Warning("Failed Parsing OSC variable in \"{valuesText}\" => type \"{typeText}\" for value \"{valueText}\"", valuesText, type, value);
                    continue;
            }
        }

        _logger.Verbose("Parsed {parsedCount}/{totalCount} OSC variables from \"{valuesText}\"", parsedVariables.Count, variableMatches.Count, valuesText);
        return parsedVariables;
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
            if (_cts.IsCancellationRequested)
            {
                _logger.Verbose("{taskId}: Cancelled early during step {step}", taskId, i + 1);
                return Task.CompletedTask;
            }

            var cmdInfo = commandInfos[i];
            _logger.Verbose("{taskId}: Step {step}/{cmdCount} sending {cmdInfo}", taskId, i + 1, cmdCount, cmdInfo.ToString());
            if (!_sender.SendSync(cmdInfo.Ip ?? _sender.GetDefaultIp(), cmdInfo.Port ?? _sender.GetDefaultPort(), cmdInfo.Address, cmdInfo.Arguments))
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
    public int PerformTaskCleanup()
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
    protected override void StartInternal()
    {
        _logger.Debug("Service \"started\", StartStop is only implemented for a clean shutdown");
        return;
    }

    protected override bool IsStarted()
        => !_cts.IsCancellationRequested;
    protected override bool IsProcessing()
        => IsStarted();

    public override void Restart()
    {
        _logger.Debug("Service \"restarted\", StartStop is only implemented for a clean shutdown");
    }

    public override void Stop()
    {
        _logger.Debug("Service stopping, ensuring all tasks ended");
        _cts.Cancel();

        var remain = PerformTaskCleanup();
        if (remain > 0)
        {
            _logger.Verbose("{remain} tasks are still running, waiting a timer period to allow them to safely stop", remain);
            Thread.Sleep(OSC_COMMAND_MAX_UNINTERRUPTED_WAIT + 1); //This should ensure all tasks to have a chance of exiting unless stuck
            remain = PerformTaskCleanup();
            if (remain > 0)
            {
                _logger.Verbose("{remain} tasks refused to stop in expected duration, forcing shutdown", remain);
                _runningTasks.Clear();
            }
        }

        _cts.Dispose();
        _logger.Debug("Service is stopped");
    }
    #endregion
}