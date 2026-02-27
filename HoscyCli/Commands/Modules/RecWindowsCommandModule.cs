using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Extra;

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

    [SubCommandModule(["models"], "List available windows rreognizer models")]
    public CommandResult CmdModels()
    {
        var models = _modelProvider.GetWindowsRecognizers();
        var modelText = models.Count > 0
            ? string.Join("\n", models.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available windows recognizer models:\n{modelText}");
        return CommandResult.Success;
    }

    [SubCommandModule(["selected-model"], "Recognition model to use")]
    public CommandResult CmdSelectedModel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Windows_ModelId));
    }
}