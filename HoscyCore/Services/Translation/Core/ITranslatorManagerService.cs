using HoscyCore.Services.Core;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationManagerService : ISoloModuleManagerV2<ITranslationModuleStartInfo>
{
    public TranslationResult TryTranslate(string input, out string? output);
}