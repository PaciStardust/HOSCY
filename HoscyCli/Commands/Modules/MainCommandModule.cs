using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule), Lifetime.Singleton)]
public class MainCommandModule(
    TestCommandModule testCm
) : AttributeCommandModule
{
    private readonly TestCommandModule _testCm = testCm;

    [SubCommandModule(["test"], "For testing purposes")]
    public CommandResult Test(string? args)
    {
        if (args is null) return CommandResult.MissingParameter;
        return _testCm.Execute(args);
    }
}