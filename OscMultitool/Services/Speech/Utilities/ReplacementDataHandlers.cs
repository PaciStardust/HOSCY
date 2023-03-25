using Hoscy.Models;
using System;
using System.Text.RegularExpressions;

namespace Hoscy.Services.Speech.Utilities
{
    internal readonly struct ReplacementHandler
    {
        private readonly Regex _regex;
        private readonly string _replacement;

        internal ReplacementHandler(ReplacementDataModel replacementData)
        {
            _replacement = replacementData.Replacement;

            RegexOptions rexOpt = replacementData.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
            _regex = replacementData.UseRegex
                ? new(replacementData.Text, rexOpt)
                : new($@"(?<= |\b){Regex.Escape(replacementData.Text)}(?=$| |\b)", rexOpt);
        }

        internal string Replace(string text)
            => _regex?.Replace(text, _replacement) ?? text;
    }

    internal readonly struct ShortcutHandler
    {
        private readonly Regex? _regex;
        private readonly string _text;
        private readonly bool _ignoreCase;
        internal readonly string Replacement { get; private init; }

        internal ShortcutHandler(ReplacementDataModel replacementData)
        {
            Replacement = replacementData.Replacement;
            _text = replacementData.Text;
            _ignoreCase = replacementData.IgnoreCase;

            if (replacementData.UseRegex && _text.StartsWith('^') && _text.EndsWith('$'))
                _text = _text.TrimStart('^').TrimEnd('$');

            RegexOptions rexOpt = replacementData.UseRegex ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
            _regex = replacementData.UseRegex ? new($"^{_text}$", rexOpt) : null;
        }

        internal bool Compare(string text)
        {
            if (_regex == null)
                return _text.Equals(text, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            return _regex.IsMatch(text);
        }
    }
}
