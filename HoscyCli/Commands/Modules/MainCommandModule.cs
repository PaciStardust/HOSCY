using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule), Lifetime.Singleton)]
public class MainCommandModule(
    ConfigCommandModule configCm
) : AttributeCommandModule
{
    private readonly ConfigCommandModule _configCm = configCm;

    [SubCommandModule(["config"], "Edit the config file")]
    public CommandResult Test(string? args)
    {
        if (args is null) return CommandResult.MissingParameter;
        return _configCm.Execute(args);
    }
}