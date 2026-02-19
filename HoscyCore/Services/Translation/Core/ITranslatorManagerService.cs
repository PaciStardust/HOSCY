using HoscyCore.Services.Core;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationManagerService : ISoloModuleManager<ITranslationModuleStartInfo, ITranslationModule>
{
    public TranslationResult TryTranslate(string input, out string? output);
}