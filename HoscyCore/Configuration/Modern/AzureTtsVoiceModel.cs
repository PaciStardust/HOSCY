using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyCore.Configuration.Modern;

public class AzureTtsVoiceModel : ObservableObject
{
    public const string DESC_Name = "Name of Model to be used in logging and drop-downs";
    private string _name = "New Voice";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "New Voice" : value);
    }

    public const string DESC_Voice = "Azure voice to be used";
    private string _voice = string.Empty;
    public string Voice
    {
        get => _voice;
        set => SetProperty(ref _voice, value);
    }

    public const string DESC_Language = "Language of the voice";
    private string _language = string.Empty;
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public override string ToString()
        => $"{(string.IsNullOrWhiteSpace(Language) ? string.Empty : $"[{Language}] ")}{Name}";
}