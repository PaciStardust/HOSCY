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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["service-region"], nameof(ConfigModel.AzureServices_Region), ConfigModel.DESC_AzureServices_Region),
            new(["service-key"], nameof(ConfigModel.AzureServices_ApiKey), ConfigModel.DESC_AzureServices_ApiKey),
            new(["censor-profanity"], nameof(ConfigModel.AzureServices_CensorProfanity), ConfigModel.DESC_AzureServices_CensorProfanity)
        ];
    }
}