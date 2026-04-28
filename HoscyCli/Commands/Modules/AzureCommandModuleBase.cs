using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

public abstract class AzureCommandModuleBase
(
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    protected readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public abstract string ModuleName { get; }
    public abstract string ModuleDescription { get; }
    public abstract string[] ModuleCommands { get; }

    [SubCommandModule(["service-region"], "Set the Azure region")]
    public Res CmdServiceRegion()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.AzureServices_Region));
    }

    [SubCommandModule(["service-key"], "Set the Azure API key")]
    public Res CmdServiceKey()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.AzureServices_ApiKey));
    }
}