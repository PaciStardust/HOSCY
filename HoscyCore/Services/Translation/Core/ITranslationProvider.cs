using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationProvider : IStartStopSubmodule
{
    public string Metadata { get; } //todo: fix
    public TranslationResult TryTranslate(string input, out string? output);
}