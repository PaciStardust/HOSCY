namespace HoscyCore.Services.Output.Core;

[Flags]
public enum OutputSettingsFlags
{
    None = 0,
    AllowTextOutput = 1,
    AllowAudioOutput = 2,
    AllowOtherOutput = 4,
    DoTranslate = 8,
    DoPreprocessPartial = 16,
    DoPreprocessFull = 32,
    DoPreprocessAll = DoPreprocessPartial | DoPreprocessFull,
    AllowAllOutputs = AllowTextOutput | AllowAudioOutput | AllowOtherOutput,
    AllowAllTransforms = DoTranslate | DoPreprocessAll
}