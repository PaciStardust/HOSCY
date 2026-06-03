#if WINDOWS

using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecWindowsCommandModule))]
public class RecWindowsCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    ILogger logger
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly ILogger _logger = logger.ForContext<RecWindowsCommandModule>();

    public string ModuleName => "Recognition: Windows";
    public string ModuleDescription => "Configure the Windows Recognition modules";
    public string[] ModuleCommands => ["rec-windows"];

    [SubCommandModule(["models"], "List available windows recognizer models")]
    public Res CmdModels()
    {
        var models = WinApi.GetWindowsRecognizers(_logger);
        if (!models.IsOk) return ResC.Fail(models.Msg);

        var modelText = models.Value.Count > 0
            ? string.Join("\n", models.Value.Select(x => $" - {x.Name} > {x.Desc} > {x.Id}"))
            : "[NONE]";
        Console.WriteLine($"All available windows recognizer models:\n{modelText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-model"], ConfigModel.DESC_Recognition_Windows_ModelId)]
    public Res CmdSelectedModel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Windows_ModelId));
    }
}

#endif