using System;
using System.Text.RegularExpressions;
using Hoscy.Configuration.Modern;

namespace Hoscy.Services.Output.Preprocessing;

public class FullReplacementHandler
{
    private readonly Regex? _regex;
    private readonly string _text;
    private readonly bool _ignoreCase;
    private readonly string _replacement;

    public FullReplacementHandler(ReplacementDataModel replacementData)
    {
        _replacement = replacementData.Replacement;
        _text = replacementData.Text;
        _ignoreCase = replacementData.IgnoreCase;

        if (replacementData.UseRegex && _text.StartsWith('^') && _text.EndsWith('$'))
            _text = _text.TrimStart('^').TrimEnd('$');

        RegexOptions rexOpt = replacementData.UseRegex ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
        _regex = replacementData.UseRegex ? new($"^{_text}$", rexOpt) : null;
    }

    public bool Compare(string text)
    {
        if (_regex == null)
            return _text.Equals(text, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        return _regex.IsMatch(text);
    }

    public string GetReplacement()
        => _replacement;
}