using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoscy.Models.Config;

public class ReplacementDataModel : ObservableObject
{
    public ReplacementDataModel(string text, string replacement, bool ignoreCase = true)
    {
        Text = text;
        Replacement = replacement;
        IgnoreCase = ignoreCase;
    }
    public ReplacementDataModel() { }

    private string _text = "New Text";
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, string.IsNullOrWhiteSpace(value) ? "New Text" : value);
    }

    private string _replacement = "Example";
    public string Replacement
    {
        get => _replacement;
        set => SetProperty(ref _replacement, value);
    }

    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    private bool _useRegex;
    public bool UseRegex
    {
        get => _useRegex;
        set => SetProperty(ref _useRegex, value);
    }

    private bool _ignoreCase = true;
    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => SetProperty(ref _ignoreCase, value);
    }

    public override string ToString()
        => $"{(Enabled ? string.Empty : "[x] ")}{Text} ={(UseRegex ? "R" : string.Empty)}{(IgnoreCase ? string.Empty : "C")}> {Replacement}";
}