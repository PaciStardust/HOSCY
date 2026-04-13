using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(DebugCommandModule))]
public class DebugCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Debug";
    public string ModuleDescription => "Configure debugging options";
    public string[] ModuleCommands => [ "debug", "dbg" ];

    [SubCommandModule(["out-windows-cmd"], "Log on CMD on windows")]
    public Res CmdOutWindowsCmd()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaCmdOnWindows));
    }

    [SubCommandModule(["out-terminal"], "Log to terminal if executed there (Not in CLI)")]
    public Res CmdOutTerminal()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaTerminal));
    }

    [SubCommandModule(["out-follow-enabled"], "Log via file follow process in separate terminal (ex: tail)")]
    public Res CmdOutFollowEnabled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogViaTerminal));
    }

    [SubCommandModule(["out-follow-process"], "Process to start file follow command in (ex: kitty)")]
    public Res CmdOutFollowProcess()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFileFollowProcess));
    }

    [SubCommandModule(["out-follow-command"], "Command to run in the opened process (ex: \"-e tail -f [LOGFILE]\")")]
    public Res CmdOutFollowCommand()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFileFollowCommand));
    }

    [SubCommandModule(["log-severity"], "Minimum log level to log")]
    public Res CmdLogSeverity()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogMinimumSeverity));
    }

    [SubCommandModule(["log-fiters"], "Filters for logging")]
    public Res CmdLogFilters()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogFilters));
    }

    [SubCommandModule(["log-verbose-extra"], "Enable extra verbose logging")]
    public Res CmdLogVerboseExtra()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Debug_LogVerboseExtra));
    }
}