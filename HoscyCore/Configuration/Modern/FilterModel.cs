using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyCore.Configuration.Modern;

public class FilterModel : ObservableObject
{
    public FilterModel(string name, string filterString)
    {
        Name = name;
        FilterString = filterString;
    }
    public FilterModel() { }

    private const string NO_FILTER_NAME = "Unnamed Filter";
    private string _name = NO_FILTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_FILTER_NAME : value);
    }

    private const string FILTER_TEXT_PLACEHOLDER = "Filter Text";
    private string _filterString = FILTER_TEXT_PLACEHOLDER;
    public string FilterString
    {
        get => _filterString;
        set
        {
            SetProperty(ref _filterString, string.IsNullOrWhiteSpace(value) ? "Filter Text" : value);
            TryUpdateRegex();
        }
    }

    private bool _enabled = true;
    public bool Enabled //todo: [TEST]
    {
        get => _enabled;
        set
        {
            SetProperty(ref _enabled, value);
            TryUpdateRegex();
        }
    }

    private bool _ignoreCase = true;
    public bool IgnoreCase //todo: [TEST]
    {
        get => _ignoreCase;
        set
        {
            SetProperty(ref _ignoreCase, value);
            TryUpdateRegex();
        }
    }

    private bool _useRegex = false;
    public bool UseRegex //todo: [TEST]
    {
        get => _useRegex;
        set
        {
            SetProperty(ref _useRegex, value);
            TryUpdateRegex();
        }
    }

    private Regex? _regex;
    private string _lastException = string.Empty;
    protected void TryUpdateRegex()
    {
        try
        {
            _regex = _useRegex
                ? new(_filterString, _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant)
                : null;
        }
        catch (Exception ex)
        {
            _regex = null;
            _lastException = ex.Message;
            Enabled = false;
        }
    }
    public string GetLastException()
        => _lastException;
    public bool IsValid => _regex != null;

    public bool Matches(string compare)
    {
        if (!_enabled)
            return false;

        if (_regex == null)
            return compare.Contains(_filterString, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        return _regex.IsMatch(compare);
    }

    public override string ToString()
    {
        return $"{(Enabled ? string.Empty : "[x] ")}{_name} ={(_useRegex ? "R" : string.Empty)}{(_ignoreCase ? string.Empty : "C")}> {_filterString}";
    }
}