using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(TestCommandModule), Lifetime.Singleton)]
public class TestCommandModule(ReflectPropEditCommandModule modCm) : AttributeCommandModule
{
    private readonly ReflectPropEditCommandModule _modCm = modCm;

    [SubCommandModule(["echo"], "Echo a message")]
    public CommandResult Echo(string? message)
    {
        Console.WriteLine($"Echo: {message ?? "Mrow"}");
        return CommandResult.Success;
    }

    [SubCommandModule(["osc-afk-duration"], "Set OSC afk duration")]
    public CommandResult OscAfkDuration(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return CommandResult.MissingParameter;
        return _modCm.ExecutePrependArgs(message, nameof(ConfigModel.Osc_Afk_ShowDuration));
    }
}