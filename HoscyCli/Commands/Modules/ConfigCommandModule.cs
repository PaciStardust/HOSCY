using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ConfigCommandModule), Lifetime.Singleton)]
public class ConfigCommandModule(ReflectPropEditCommandModule reflectionCm, ConfigModel config, ILogger logger) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectionCm = reflectionCm;
    private readonly string[] _allProps = ReflectPropEditCommandModule.GetAllConfigValues();
    private readonly ConfigModel _config = config;
    private readonly ILogger _logger = logger.ForContext<ConfigCommandModule>();

    public string ModuleName => "Config";
    public string ModuleDescription => "Configure the entire config file";
    public string[] ModuleCommands => ["config", "cfg"];

    [SubCommandModule(["list", "l", "all", "a"], "Get all editable properties (additional text filters)")]
    public Res GetAll(string? filter)
    {
        var allProps = ReflectPropEditCommandModule.GetAllConfigValues();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            allProps = allProps.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        var propText = allProps.Select(x => $"{x} ({_allProps.IndexOf(x)})");

        var printText = allProps.Length > 0
            ? "All properties:\n" + string.Join(", ", propText)
            : "No properties found";
        Console.WriteLine(printText);
        return ResC.Ok();
    }

    [SubCommandModule(["pick"], "Pick an editable property")]
    public Res PickOne(string? message)
    {
        if (OnEmpty(message)) return ResC.Fail("You must specify a property to edit", ResMsgLvl.Info);

        var (command, parameters) = Util.SplitAtFirstSpace(message);
        if (string.IsNullOrWhiteSpace(parameters))
        {
            parameters = "set";
        }

        var match = int.TryParse(command, out var index) 
            ? index < 0 || index >= _allProps.Length 
                ? null 
                : _allProps[index]
            : _allProps.FirstOrDefault(x => x.Equals(command, StringComparison.OrdinalIgnoreCase));
        if (match is null) return ResC.Fail("No property match found for input");
        return _reflectionCm.Execute(parameters, match);
    }

    [SubCommandModule(["save", "s"], "Save the config file")]
    public Res Save()
    {
        var success = _config.TrySave(PathUtils.PathConfigFolder,ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
        if (!success) return ResC.Fail("Saving failed!");
        Console.WriteLine("Config saved");
        return ResC.Ok();
    }
}