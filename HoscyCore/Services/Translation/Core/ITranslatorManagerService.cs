using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslatorManagerService : IAutoStartStopService
{
    public IReadOnlyList<ITranslationProviderStartInfo> GetProviderInfos();
    public ITranslationProviderStartInfo? GetCurrentProviderInfo();
    public ServiceStatus GetCurrentProviderStatus();

    public void RefreshProvider();
    public bool RestartCurrentProvider();

    public TranslationResult TryTranslate(string input, out string? output);
}