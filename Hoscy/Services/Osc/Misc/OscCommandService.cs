using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)]
public partial class OscCommandService(ILogger logger) : IOscCommandService
{
    private readonly ILogger _logger = logger.ForContext<OscCommandService>();

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
        if (commandMatches == null || commandMatches.Count == 0)
        {
            _logger.Warning("Failed parsing OSC command, it did not match the filter");
            return OscCommandState.Malformed;
        }

        _logger.Debug("{commandMatchCount} OSC command matches found in string \"{commandString}\", parsing individual commands", commandMatches.Count, commandString);
        var commandMessages = new List<(OscMessage, int)>();
        foreach (Match commandMatch in commandMatches)
        {
            var output = ParseOscCommandString(commandMatch);
            if (output == null)
                return OscCommandState.Malformed;

            commandMessages.Add(output.Value);
        }

        if (commandMessages.Count == 0)
        {
            _logger.Warning("Failed to find any command messages to execute");
            return OscCommandState.Malformed;
        }

        var threadId = "ST-" + Guid.NewGuid().ToString().Split('-')[0];
        _logger.Debug("Parsed {commandMessageCount} OSC command messages from string \"{commandString}\", executing as id {threadId}",
            commandMessages.Count, commandString, threadId);
        //todo: keep track of these and cancel them?
        Task.Run(() => ExecuteOscCommands(threadId, commandMessages));
        return OscCommandState.Success;
    }

    private (OscMessage, int)? ParseOscCommandString(Match commandMatch)
    {
        throw new NotImplementedException();
    }

    private void ExecuteOscCommands(string threadId, List<(OscMessage, int)> commandMessages)
    {
        throw new NotImplementedException();
    }
}