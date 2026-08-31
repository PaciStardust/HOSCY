using HoscyCli.Commands.Core;
using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

public abstract class SoloModuleManagerCommandModuleBase<Tmanager, Tstartinfo> 
(
    ReflectPropEditCommandModule reflectCm,
    Tmanager manager
)
: AttributeCommandModule 
    where Tmanager: ISoloModuleManager<Tstartinfo> 
    where Tstartinfo: class, ISoloModuleStartInfo
{
    protected readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    protected readonly Tmanager _manager = manager;

    #region Modules
    [SubCommandModule(["modules"], "Lists available modules")] 
    public Res CmdModules()
    {
        var modules = _manager.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available modules:\n{moduleText}");
        return ResC.Ok();
    }
    #endregion
    
    #region Start / Stop
    [SubCommandModule(["status"], "Get the module status")]
    public Res CmdStatus()
    {
        var info = _manager.GetCurrentModuleInfo();
        var infoText = info is null ? "None" : info.IsOk ? info.Value.Name : "ERROR";

        string[] textSplit = [
            $"Manager: {_manager.GetCurrentStatus()}",
            $"Module ({infoText}): {_manager.GetCurrentModuleStatus()}",
        ];
        var text = string.Join("\n", textSplit);
        Console.WriteLine(text);
        return ResC.Ok();
    }
    protected virtual string[] GetAdditionalStatusLines()
    {
        return [];
    }

    [SubCommandModule(["start"], "Start module")]
    public Res CmdStart()
    {
        return _manager.StartModule();
    }

    [SubCommandModule(["stop"], "Stop module")]
    public Res CmdStop()
    {
        return _manager.StopModule();
    }

    [SubCommandModule(["refresh"], "Refresh module")]
    public Res CmdRefresh()
    {
        return _manager.RefreshModule();
    }
    #endregion
}