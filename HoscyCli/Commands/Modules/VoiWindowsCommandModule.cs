#if WINDOWS

using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(VoiWindowsCommandModule))]
public class VoiWindowsCommandModule
(   
    ReflectPropEditCommandModule reflectCm,
    ILogger logger
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly ILogger _logger = logger.ForContext<VoiWindowsCommandModule>();

    public string ModuleName => "Voice - Azure";
    public string ModuleDescription => "Configure the azure services voice module";
    public string[] ModuleCommands => [ "voi-azure" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["selected-model"], nameof(ConfigModel.Voice_Microsoft_ModelName), ConfigModel.DESC_Voice_Microsoft_ModelName)
        ];
    }

    [SubCommandModule(["windows-models"], "List windows voice models")]
    public Res CmdWindowsModels()
    {
        var models = WinApi.GetWindowsVoices(_logger);
        if (!models.IsOk) return ResC.Fail(models.Msg);

        var modelText = models.Value.Count > 0
            ? string.Join("\n", models.Value.Select(x => $" - {x.Name} > {x.Description} > {x.Id}"))
            : "[NONE]";
        Console.WriteLine($"All available windows voice models:\n{modelText}");
        return ResC.Ok();
    }
}

#endif