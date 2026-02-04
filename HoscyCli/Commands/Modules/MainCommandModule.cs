using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule), Lifetime.Singleton)] //todo: [REFACTOR] Fix this loading, ensure all is configurable
public class MainCommandModule(
    ConfigCommandModule configCm,
    ServiceManagerCommandModule serviceCm,
    OscCommandModule oscCm,
    AfkCommandModule afkCm,
    CounterCommandModule counterCm,
    AudioCommandModule audioCm,
    InputCommandModule exInputCm,
    PreprocessingCommandModule preprocessCm
) : AttributeCommandModule
{
    private readonly ConfigCommandModule _configCm = configCm;
    private readonly ServiceManagerCommandModule _serviceCm = serviceCm;
    private readonly OscCommandModule _oscCm = oscCm;
    private readonly AfkCommandModule _afkCm = afkCm;
    private readonly CounterCommandModule _counterCm = counterCm;
    private readonly AudioCommandModule _audioCm = audioCm;
    private readonly InputCommandModule _exInputCm = exInputCm;
    private readonly PreprocessingCommandModule _preprocessCm = preprocessCm;

    [SubCommandModule(["config"], "Edit the config file")]
    public CommandResult CmdConfig(string? args)
    {
        if (OnEmpty(args, GetParameterError("config"))) return CommandResult.MissingParameter;
        return _configCm.Execute(args);
    }

    [SubCommandModule(["services"], "Manage all services")]
    public CommandResult CmdServices(string? args)
    {
        if (OnEmpty(args, GetParameterError("services"))) return CommandResult.MissingParameter;
        return _serviceCm.Execute(args);
    }

    [SubCommandModule(["osc"], "Manage OSC")]
    public CommandResult CmdOsc(string? args)
    {
        if (OnEmpty(args, GetParameterError("osc"))) return CommandResult.MissingParameter;
        return _oscCm.Execute(args);
    }

    [SubCommandModule(["afk"], "Manage AFK")]
    public CommandResult CmdAfk(string? args)
    {
        if (OnEmpty(args, GetParameterError("afk"))) return CommandResult.MissingParameter;
        return _afkCm.Execute(args);
    }

    [SubCommandModule(["counters"], "Manage counters")]
    public CommandResult CmdCounters(string? args)
    {
        if (OnEmpty(args, GetParameterError("counters"))) return CommandResult.MissingParameter;
        return _counterCm.Execute(args);
    }

    [SubCommandModule(["audio"], "Manage audio")]
    public CommandResult CmdAudio(string? args)
    {
        if (OnEmpty(args, GetParameterError("audio"))) return CommandResult.MissingParameter;
        return _audioCm.Execute(args);
    }

    [SubCommandModule(["input"], "Manage inputs")]
    public CommandResult CmdExIn(string? args)
    {
        if (OnEmpty(args, GetParameterError("input"))) return CommandResult.MissingParameter;
        return _exInputCm.Execute(args);
    }

    [SubCommandModule(["preprocessing"], "Manage preprocessing")]
    public CommandResult CmdPreprocessing(string? args)
    {
        if (OnEmpty(args, GetParameterError("preprocessing"))) return CommandResult.MissingParameter;
        return _preprocessCm.Execute(args);
    }

    private string GetParameterError(string commandName) 
        => $"Subcommand required for {commandName} command";
}