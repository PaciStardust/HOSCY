using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecWindowsCommandModule))]
public class RecWindowsCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    IRecognitionModelProviderService modelProvider
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IRecognitionModelProviderService _modelProvider = modelProvider;

    public string ModuleName => "Recognition: Windows";
    public string ModuleDescription => "Configure the Windows Recognition modules";
    public string[] ModuleCommands => ["rec-windows"];

    [SubCommandModule(["models"], "List available windows recognizer models")]
    public Res CmdModels()
    {
        var models = _modelProvider.GetWindowsRecognizers();
        if (!models.IsOk) return ResC.Fail(models.Msg);

        var modelText = models.Value.Count > 0
            ? string.Join("\n", models.Value.Select(x => $" - {x.Name} > {x.Desc} > {x.Id}"))
            : "[NONE]";
        Console.WriteLine($"All available windows recognizer models:\n{modelText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-model"], "Recognition model to use")]
    public Res CmdSelectedModel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Windows_ModelId));
    }
}