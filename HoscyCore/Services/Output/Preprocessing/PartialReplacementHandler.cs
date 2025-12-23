using System.Text.RegularExpressions;
using HoscyCore.Configuration.Modern;

namespace HoscyCore.Services.Output.Preprocessing;

public class PartialReplacementHandler
{
    private readonly Regex _regex;
    private readonly string _replacement;

    public PartialReplacementHandler(ReplacementDataModel replacementData)
    {
        _replacement = replacementData.Replacement;

        var rexOpt = replacementData.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
        _regex = replacementData.UseRegex
            ? new(replacementData.Text, rexOpt)
            : new($@"(?<=^| |\b){Regex.Escape(replacementData.Text)}(?=$| |\b)", rexOpt);
    }

    public string Replace(string text)
    {
        return _regex?.Replace(text, _replacement) ?? text;
    }
}