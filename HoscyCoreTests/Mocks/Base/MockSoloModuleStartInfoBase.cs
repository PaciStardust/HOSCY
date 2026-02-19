using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockSoloModuleStartInfoBase : ISoloModuleStartInfo
{
    public string Name { get; set; } = "Mock";

    public string Description { get; set; } = "Mock";

    public Type ModuleType { get; set; } = typeof(MockSoloModuleStartInfoBase);
}