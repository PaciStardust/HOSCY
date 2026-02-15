using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslationProviderStartInfo : IService
{
    public string Name { get; }
    public string Description { get; }
    public Type ProviderType { get; }
}