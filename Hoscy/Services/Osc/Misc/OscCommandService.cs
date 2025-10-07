using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Osc.SendReceive;
using Serilog;

namespace Hoscy.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)]
public partial class OscCommandService(ILogger logger, IOscQueryService oscQuery, IOscSendService sender) : StartStopServiceBase, IOscCommandService
{
    private readonly ILogger _logger = logger.ForContext<OscCommandService>();
    private readonly IOscQueryService _oscQuery = oscQuery;
    private readonly IOscSendService _sender = sender;
    private readonly List<Task> _runningTasks = []; //todo: use
    private readonly CancellationTokenSource _cts = new();

    [GeneratedRegex(
        @"\[ *(?<address>(?:\/[^\ #\*,\/?\[\]\{\}]+)+)(?<values>(?: +\[(?:[fF]\]-?[0-9]+(?:\.[0-9]+)?|[iI]\]\-?[0-9]+|[sS]\]""[^""]*""|[bB]\](?:[tT]rue|[fF]alse)))+)(?: +(?:(?<ip>(?:(?:25[0-5]|(?:2[0-4]|1\d|[1-9]|)\d)\.?\b){4}):(?<port>[0-9]{1,5})|""(?<target>[^""]*)""))?(?: +[wW](?<wait>[0-9]+))? *\]",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex OscCommandIdentifierRegex();

    [GeneratedRegex(
        @" +\[(?<type>[iIfFbBsS])\](?:""(?<value>[^""]*)""|(?<value>[a-zA-Z]+|[0-9\.\-]*))",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex OscParameterExtractorRegex();

    #region Funtionality
    private const string OSC_COMMAND_IDENTIFIER = "[OSC]";
    public string GetCommandIdentifier()
        => OSC_COMMAND_IDENTIFIER;

    public bool DetectCommand(string commandString)
    {
        return commandString.StartsWith(OSC_COMMAND_IDENTIFIER, StringComparison.OrdinalIgnoreCase);
    }

    public OscCommandState DetectAndHandleCommand(string commandString)
    {
        if (_cts.IsCancellationRequested) return OscCommandState.Shutdown;

        _logger.Verbose("Performing OSC command check on string \"{commandString}\"", commandString);
        if (!DetectCommand(commandString))
        {
            _logger.Verbose("Could not find OSC command identifier in string \"{commandString}\"", commandString);
            return OscCommandState.NotCommand;
        }
        _logger.Debug("Detected OSC command identifier, attempting to parse string \"{commandString}\"", commandString);

        var commandMatches = OscCommandIdentifierRegex().Matches(commandString);
        if (commandMatches is null || commandMatches.Count == 0)
        {
            _logger.Warning("Failed parsing OSC command, it did not match the filter");
            return OscCommandState.Malformed;
        }

        _logger.Debug("{commandMatchCount} OSC command matches found in string \"{commandString}\", parsing individual commands", commandMatches.Count, commandString);
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
        //todo: keep track of these and cancel them?
        Task.Run(() => ExecuteOscCommands(threadId, commandInfos));
        return OscCommandState.Success;
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
            var target = _oscQuery.GetServiceAddressByName(targetText);
            if (!target.HasValue)
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", specified target \"{target}\" not found", commandMatch.Value, targetText);
                return null;
            }
            if (!ushort.TryParse(target.Value.Item2.ToString(), out var parsedPortTmp)) //todo: fix this conversion
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", unable to parse port {port}", commandMatch.Value, target.Value.Item2);
                return null;
            }
            ipText = target.Value.Item1;
            parsedPort = parsedPortTmp;
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
        var variableMatches = OscParameterExtractorRegex().Matches(valuesText);
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
                _logger.Debug("{taskId}: Cancelled early during step {step}", taskId, i + 1);
                return Task.CompletedTask;
            }

            var cmdInfo = commandInfos[i];
            _logger.Debug("{taskId}: Step {step}/{cmdCount} sending {cmdInfo}", taskId, i + 1, cmdCount, cmdInfo.ToString());
            if (!_sender.SendSync(cmdInfo.Ip ?? _sender.GetDefaultIp(), cmdInfo.Port ?? _sender.GetDefaultPort(), cmdInfo.Address, cmdInfo.Arguments))
            {
                _logger.Warning("{taskId}: Step {step}/{cmdCount} with info {cmdInfo} failed to send", taskId, i + 1, cmdCount, cmdInfo.ToString());
                return Task.CompletedTask;
            }
            if (i != cmdCount - 1 && cmdInfo.Wait.HasValue && cmdInfo.Wait.Value > 0)
            {
                _logger.Debug("{taskId}: Step {step}/{cmdCount} waiting for {timeout}ms after execution", taskId, i + 1, cmdCount, cmdInfo.Wait);
                Task.Delay(cmdInfo.Wait.Value).Wait();
            }

        }
        _logger.Debug("{taskId}: Finished processing {cmdCount} subcommands", taskId, cmdCount);
        return Task.CompletedTask;
    }
    #endregion

    #region Task Management
    public void PerformTaskCleanup()
    {
        var currentCount = _runningTasks.Count;
        _logger.Debug("Performing Task Cleanup, currently {currentCount} in list", currentCount);
        for (var i = currentCount; i > -1; i--)
        {
            if (_runningTasks[i].IsCompleted)
            {
                _runningTasks.RemoveAt(i);
            }
        }
        _logger.Debug("Performed Task Cleanup, currently {oldCount} => {currentCount} in list", currentCount, _runningTasks.Count);
    }
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Information("Service \"started\", StartStop is only implemented for a clean shutdown");
        return;
    }

    public override bool IsRunning()
    {
        return !_cts.IsCancellationRequested;
    }

    public override bool TryRestart()
    {
        _logger.Information("Service \"restarted\", StartStop is only implemented for a clean shutdown");
        return true;
    }

    public override void Stop()
    {
        throw new NotImplementedException();
    }
    #endregion
}