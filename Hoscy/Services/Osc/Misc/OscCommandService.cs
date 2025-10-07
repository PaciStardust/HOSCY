using System.Text.RegularExpressions;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscCommandService), Lifetime.Singleton)]
public partial class OscCommandService : IOscCommandService
{
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
        throw new System.NotImplementedException();
    }

    public bool DetectAndHandleCommand(string commandString)
    {
        throw new System.NotImplementedException();
    }
}