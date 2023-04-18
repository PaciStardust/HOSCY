using System;
using System.Text.RegularExpressions;

namespace Hoscy.Models
{
    /// <summary>
    /// THIS CLASS IS USED IN CONFIG - IT CAN NOT LOG
    /// </summary>
    internal class FilterModel
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

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateRegex();
            }
        }
        private bool _enabled = true;

        public bool IgnoreCase
        {
            get => _ignoreCase;
            set
            {
                _ignoreCase = value;
                UpdateRegex();
            }
        }
        private bool _ignoreCase = true;

        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                _useRegex = value;
                UpdateRegex();
            }
        }
        private bool _useRegex = false;

        private Regex? _regex;

        public FilterModel(string name, string filterString)
        {
            Name = name;
            FilterString = filterString;
        }
        public FilterModel() { }

        protected void UpdateRegex()
        {
            try
            {
                _regex = _useRegex
                    ? new(_filterString, _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant)
                    : null;
            }
            catch
            {
                _regex = null;
                _enabled = false;
            }
        }

        internal bool Matches(string compare)
        {
            if (!_enabled)
                return false;

            if (_regex == null)
                return compare.Contains(_filterString, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            return _regex.IsMatch(compare);
        }

        public override string ToString()
            => $"{(Enabled ? string.Empty : "[x] ")}{_name} ={(_useRegex ? "R" : string.Empty)}{(_ignoreCase ? string.Empty : "C")}> {_filterString}";
    }
}
