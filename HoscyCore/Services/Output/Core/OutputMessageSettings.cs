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
    SkipProcessorsWithTextOutput = 1,
    SkipProcessorsWithAudioOutput = 2,
    SkipProcessorsWithOtherOutput = 4,
    DoNotTranslate = 8,
    DoNotPreprocess = 16
}