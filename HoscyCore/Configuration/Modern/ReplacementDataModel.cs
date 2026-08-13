using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyCore.Configuration.Modern;

public class ReplacementDataModel : ObservableObject
{
    public ReplacementDataModel(string text, string replacement, bool ignoreCase = true)
    {
        Text = text;
        Replacement = replacement;
        IgnoreCase = ignoreCase;
    }
    public ReplacementDataModel() { }

    public const string DESC_Text = "Text that is being searched for replacing";
    private string _text = "New Text";
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, string.IsNullOrWhiteSpace(value) ? "New Text" : value);
    }

    public const string DESC_Replacement = "Text that the original text will be replaced with";
    private string _replacement = "Example";
    public string Replacement
    {
        get => _replacement;
        set => SetProperty(ref _replacement, value);
    }

    public const string DESC_Enabled = "Sets if replacement will trigger";
    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public const string DESC_UseRegex = "Set search text to be a regular expression";
    private bool _useRegex;
    public bool UseRegex
    {
        get => _useRegex;
        set => SetProperty(ref _useRegex, value);
    }

    public const string DESC_IgnoreCase = "Set search text to be case insensitive";
    private bool _ignoreCase = true;
    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public override string ToString()
    {
        return $"{(Enabled ? string.Empty : "[x] ")}{Text} ={(UseRegex ? "R" : string.Empty)}{(IgnoreCase ? string.Empty : "C")}> {Replacement}";
    }
}