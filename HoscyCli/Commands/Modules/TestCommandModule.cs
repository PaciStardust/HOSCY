using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(TestCommandModule), Lifetime.Singleton)]
public class TestCommandModule : AttributeCommandModule
{
    [SubCommandModule(["echo"], "Echo a message.")]
    public CommandResult Echo(string? message)
    {
        Console.WriteLine($"Echo: {message ?? "Mrow"}");
        return CommandResult.Success;
    }
}