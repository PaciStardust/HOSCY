using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslationProviderBase : StartStopSubmoduleBase, ITranslationProvider
{
    public abstract TranslationResult TryTranslate(string input, out string? output);
}