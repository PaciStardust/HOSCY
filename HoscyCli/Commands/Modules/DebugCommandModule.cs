using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(DebugCommandModule))]
public class DebugCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Debug";
    public string ModuleDescription => "Configure debugging options";
    public string[] ModuleCommands => [ "debug", "dbg" ];

    [SubCommandModule(["out-windows-cmd"], "Log on CMD on windows")]
    public CommandResult CmdOutWindowsCmd()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaCmdOnWindows));
    }

    [SubCommandModule(["out-terminal"], "Log to terminal if executed there (Not in CLI)")]
    public CommandResult CmdOutTerminal()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaTerminal));
    }

    [SubCommandModule(["out-follow-enabled"], "Log via file follow process in separate terminal (ex: tail)")]
    public CommandResult CmdOutFollowEnabled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaTerminal));
    }

    [SubCommandModule(["out-follow-process"], "Process to start file follow command in (ex: kitty)")]
    public CommandResult CmdOutFollowProcess()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFileFollowProcess));
    }

    [SubCommandModule(["out-follow-command"], "Command to run in the opened process (ex: \"-e tail -f [LOGFILE]\")")]
    public CommandResult CmdOutFollowCommand()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFileFollowCommand));
    }

    [SubCommandModule(["log-severity"], "Minimum log level to log")]
    public CommandResult CmdLogSeverity()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogMinimumSeverity));
    }

    [SubCommandModule(["log-fiters"], "Filters for logging")]
    public CommandResult CmdLogFilters()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFilters));
    }
}