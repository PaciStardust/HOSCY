using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks;

public class MockOutputHandlerStartInfo : IOutputHandlerStartInfo
{
    public required Type HandlerType { get; set; }
    
    public bool Enabled { get; set; } = false;

    public bool ShouldBeEnabled()
        => Enabled;
}