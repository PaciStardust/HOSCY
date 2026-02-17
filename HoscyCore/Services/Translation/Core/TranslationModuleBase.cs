using HoscyCore.Services.Core;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslationModuleBase : StartStopModuleBase, ITranslationModule
{
    public abstract TranslationResult TryTranslate(string input, out string? output);
}