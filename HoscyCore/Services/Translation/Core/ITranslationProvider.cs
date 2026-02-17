using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationProvider : IStartStopModule
{
    public TranslationResult TryTranslate(string input, out string? output);
}