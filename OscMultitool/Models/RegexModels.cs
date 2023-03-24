using System;
using System.Text.RegularExpressions;

namespace Hoscy.Models
{
    internal abstract class RegexFilteredModelBase
    {
        public bool Enabled { get; set; } = true;
        public bool IgnoreCase
        {
            get => _ignoreCase;
            set
            {
                _ignoreCase = value;
                UpdateRegex();
            }
        }
        protected bool _ignoreCase = true;
        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                _useRegex = value;
                UpdateRegex();
            }
        }
        protected bool _useRegex = false;

        protected Regex? _regex;

        protected abstract void UpdateRegex();
    }

    internal class FilterModel : RegexFilteredModelBase
    {
        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Filter" : value;
        }
        private string _name = "Unnamed Filter";
        public string FilterString
        {
            get => _filterString;
            set
            {
                _filterString = string.IsNullOrWhiteSpace(value) ? "Filter Text" : value;
                UpdateRegex();
            }
        }
        private string _filterString = "Filter Text";

        public FilterModel(string name, string filterString)
        {
            Name = name;
            FilterString = filterString;
        }
        public FilterModel() { }

        protected override void UpdateRegex()
            => _regex = _useRegex ? new(_filterString, _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant) : null;

        internal bool Matches(string compare)
        {
            if (_regex == null)
                return compare.Contains(_filterString, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            return _regex.IsMatch(compare);
        }

        public override string ToString()
            => $"{(Enabled ? "" : "[x] ")}{_name} ={(_useRegex ? "[R]" : string.Empty)}{(_ignoreCase ? string.Empty : "[C]")}> {_filterString}";
    }

    internal abstract class RegexTextChangingModelBase : RegexFilteredModelBase
    {
        public string Text
        {
            get => _text;
            set
            {
                _text = string.IsNullOrWhiteSpace(value) ? "New Text" : value;
                UpdateRegex();
            }
        }
        protected string _text = "New Text";

        public string Replacement { get; set; } = "Example";

        public RegexTextChangingModelBase(string text, string replacement) : base()
        {
            Text = text;
            Replacement = replacement;
        }
        public RegexTextChangingModelBase()
        {
            _ignoreCase = true;
        }

        public override string ToString()
            => $"{(Enabled ? "" : "[x] ")}{Text} ={(_useRegex ? "[R]" : string.Empty)}{(_ignoreCase ? string.Empty : "[C]")}> {Replacement}";
    }

    internal class ReplacementModel : RegexTextChangingModelBase //todo: test
    {
        protected override void UpdateRegex()
        {
            RegexOptions rexOpt = _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
            _regex = _useRegex ? new(_text, rexOpt) : new($@"(?<= |\b){Regex.Escape(_text)}(?=$| |\b)", rexOpt);
        }

        public ReplacementModel(string text, string replacement)
        {
            Text = text;
            Replacement = replacement;
        }
        public ReplacementModel() { }

        internal string Replace(string text)
            => _regex?.Replace(text, Replacement) ?? text;
    }

    internal class ShortcutModel : RegexTextChangingModelBase //todo: test, unify to one editor window
    {
        protected override void UpdateRegex()
        {
            if (UseRegex && _text.StartsWith('^') && _text.EndsWith('$'))
                _text = _text.TrimStart('^').TrimEnd('$');

            RegexOptions rexOpt = _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant;
            _regex = UseRegex ? new($"^{_text}$", rexOpt) : null;
        }

        public ShortcutModel(string text, string replacement)
        {
            Text = text;
            Replacement = replacement;
        }
        public ShortcutModel() { }

        internal bool Compare(string text)
        {
            if (_regex == null)
                return Text.Equals(text, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            return _regex.IsMatch(text);
        }
    }
}
