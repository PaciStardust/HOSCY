using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOutputHandlerStartInfo : IOutputHandlerStartInfo
{
    public required Type ModuleType { get; set; }
    
    public bool Enabled { get; set; } = false;

    public bool ShouldBeEnabled()
        => Enabled;
}