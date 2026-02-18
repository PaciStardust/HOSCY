using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockSoloModuleStartInfoBase : ISoloModuleStartInfo
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required Type ModuleType { get; init; }
}