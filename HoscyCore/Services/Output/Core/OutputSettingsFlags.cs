namespace HoscyCore.Services.Output.Core;

[Flags]
public enum OutputSettingsFlags
{
    None = 0,
    AllowTextOutput = 1,
    AllowAudioOutput = 2,
    AllowOtherOutput = 4,
    DoTranslate = 8,
    DoPreprocess = 16,
    AllowAllOutputs = AllowTextOutput | AllowAudioOutput | AllowOtherOutput,
    AllowAllTransforms = DoTranslate | DoPreprocess
}