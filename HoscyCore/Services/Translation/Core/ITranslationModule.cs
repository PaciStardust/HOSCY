using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationModule : IStartStopModule
{
    public TranslationResult TryTranslate(string input, out string? output);
}