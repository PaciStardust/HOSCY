using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule))]
public class MainCommandModule(IContainerBulkLoader<ICoreCommandModule> moduleLoader) : AttributeCommandModule
{
    private readonly IContainerBulkLoader<ICoreCommandModule> _moduleLoader = moduleLoader;

    protected override void AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, CommandResult> Func)> list)
    {
        var coreModules = _moduleLoader.GetInstances();
        foreach(var module in coreModules)
        {
            var subCommandProxy = new SubCommandModuleAttribute(module.ModuleCommands, module.ModuleDescription);
            list.Add((subCommandProxy, (x) => ExecuteModule(module, x)));
        }
    }

    private CommandResult ExecuteModule(ICoreCommandModule module, string? args)
    {
        if (OnEmpty(args, $"Subcommand required for {module.ModuleName} command")) return CommandResult.MissingParameter;
        return module.Execute(args);
    }
}