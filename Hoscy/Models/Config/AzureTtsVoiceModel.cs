using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoscy.Models.Config;

public class AzureTtsVoiceModel : ObservableObject
{
    private string _name = "New Voice";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "New Voice" : value);
    }

    private string _voice = string.Empty;
    public string Voice
    {
        get => _voice;
        set => SetProperty(ref _voice, value);
    }

    private string _language = string.Empty;
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public override string ToString()
        => $"{(string.IsNullOrWhiteSpace(Language) ? string.Empty : $"[{Language}] ")}{Name}";
}