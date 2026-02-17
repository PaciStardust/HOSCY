using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public class MockTranslationModuleStartInfo : ITranslationModuleStartInfo
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required Type ModuleType { get; init; }
}