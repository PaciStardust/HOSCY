using HoscyCore.Services.Core;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationModuleStartInfo : ISoloModuleStartInfo
{
    public TranslationModuleConfigFlags ConfigFlags { get; }
}

[Flags]
public enum TranslationModuleConfigFlags
{
    None = 0b0,
    Api = 0b1,
    Windows = 0b10
}

public interface ITranslationModule : IStartStopModule
{
    public TranslationResult TryTranslate(string input, out string? output);
}