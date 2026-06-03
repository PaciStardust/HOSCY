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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["out-windows-cmd"], nameof(ConfigModel.Debug_LogViaCmdOnWindows), ConfigModel.DESC_Debug_LogViaCmdOnWindows),
            new(["out-terminal"], nameof(ConfigModel.Debug_LogViaTerminal), ConfigModel.DESC_Debug_LogViaTerminal),
            new(["out-follow-enabled"], nameof(ConfigModel.Debug_LogViaTerminal), ConfigModel.DESC_Debug_LogViaTerminal),
            new(["out-follow-process"], nameof(ConfigModel.Debug_LogFileFollowProcess), ConfigModel.DESC_Debug_LogFileFollowProcess),
            new(["out-follow-command"], nameof(ConfigModel.Debug_LogFileFollowCommand), ConfigModel.DESC_Debug_LogFileFollowCommand),
            new(["log-severity"], nameof(ConfigModel.Debug_LogMinimumSeverity), ConfigModel.DESC_Debug_LogMinimumSeverity),
            new(["log-fiters"], nameof(ConfigModel.Debug_LogFilters), ConfigModel.DESC_Debug_LogFilters),
            new(["log-verbose-extra"], nameof(ConfigModel.Debug_LogVerboseExtra), ConfigModel.DESC_Debug_LogVerboseExtra)
        ];
    }
}