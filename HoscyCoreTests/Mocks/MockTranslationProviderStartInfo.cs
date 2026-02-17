using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public class MockTranslationProviderStartInfo : ITranslationProviderStartInfo
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required Type ProviderType { get; init; }
}