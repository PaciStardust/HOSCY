using HoscyCore.Services.Core;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationManagerService : ISoloModuleManager<ITranslationModuleStartInfo>
{
    public TranslationResult TryTranslate(string input, out string? output);
}