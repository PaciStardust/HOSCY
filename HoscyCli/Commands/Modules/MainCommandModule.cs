using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule), Lifetime.Singleton)]
public class MainCommandModule(
    ConfigCommandModule configCm,
    ServiceManagerCommandModule serviceCm
) : AttributeCommandModule
{
    private readonly ConfigCommandModule _configCm = configCm;
    private readonly ServiceManagerCommandModule _serviceCm = serviceCm;

    [SubCommandModule(["config"], "Edit the config file")]
    public CommandResult Config(string? args)
    {
        if (string.IsNullOrWhiteSpace(args)) return CommandResult.MissingParameter;
        return _configCm.Execute(args);
    }

    [SubCommandModule(["services"], "Manage all services")]
    public CommandResult Services(string? args)
    {
        if (string.IsNullOrWhiteSpace(args)) return CommandResult.MissingParameter;
        return _serviceCm.Execute(args);
    }
}