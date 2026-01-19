namespace HoscyCore.Services.Output.Core;

[Flags]
public enum OutputSettingsFlags
{
    NotSet = 0,
    SkipProcessorsWithTextOutput = 1,
    SkipProcessorsWithAudioOutput = 2,
    SkipProcessorsWithOtherOutput = 4,
    DoNotTranslate = 8,
    DoNotPreprocess = 16
}