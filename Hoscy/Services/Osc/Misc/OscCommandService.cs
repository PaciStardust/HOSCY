using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)]
public partial class OscCommandService(ILogger logger, IOscQueryService oscQuery) : IOscCommandService
{
    private readonly ILogger _logger = logger.ForContext<OscCommandService>();
    private readonly IOscQueryService _oscQuery = oscQuery;

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

    private const string OSC_COMMAND_IDENTIFIER = "[OSC]";
    public string GetCommandIdentifier()
        => OSC_COMMAND_IDENTIFIER;

    public bool DetectCommand(string commandString)
    {
        return commandString.StartsWith(OSC_COMMAND_IDENTIFIER, StringComparison.OrdinalIgnoreCase);
    }

    public OscCommandState DetectAndHandleCommand(string commandString)
    {
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
        string? ipText = commandMatch.Groups["ip"].Value.Length == 0 ? null: commandMatch.Groups["ip"].Value;
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

        int? parsedPort;
        if (targetText is not null)
        {
            var target = _oscQuery.GetServiceAddressByName(targetText);
            if (!target.HasValue)
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", specified target \"{target}\" not found", commandMatch.Value, targetText);
                return null;
            }
            ipText = target.Value.Item1;
            parsedPort = target.Value.Item2;
        }
        else if (portText is not null)
        {
            if (!int.TryParse(portText, out var parsedPortTmp))
            {
                _logger.Warning("Failed parsing OSC subcommand \"{subcommandString}\", unable to parse port {port}", commandMatch.Value, portText);
                return null;
            }
            parsedPort = parsedPortTmp;
        } else {
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
        return info;
    }

    private List<object> ParseOscVariables(string valuesText)
    {
        throw new NotImplementedException();
    }

    private void ExecuteOscCommands(string threadId, List<OscCommandInfo> commandInfos)
    {
        throw new NotImplementedException();
    }
}