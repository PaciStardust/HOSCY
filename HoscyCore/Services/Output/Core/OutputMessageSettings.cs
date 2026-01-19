namespace HoscyCore.Services.Output.Core;

public struct OutputMessageSettings
{
    public OutputMessageSettingsFlags Flags { get; set; }
    public string[] IgnoredProcessors { get; set; }
    public string[] IgnoredPreprocessors { get; set; }
}

[Flags]
public enum OutputMessageSettingsFlags
{
    DontSendViaText = 1,
    DontSendViaAudio = 2,
    DontSendViaOthers = 4,
    DoNotTranslate = 8,
    DoNotPreprocess = 16
}