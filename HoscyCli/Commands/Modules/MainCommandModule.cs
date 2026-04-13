using HoscyCli.Commands.Core;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(MainCommandModule))]
public class MainCommandModule(IContainerBulkLoader<ICoreCommandModule> moduleLoader) : AttributeCommandModule
{
    private readonly IContainerBulkLoader<ICoreCommandModule> _moduleLoader = moduleLoader;

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        var coreModules = _moduleLoader.GetInstances();
        if (!coreModules.IsOk) return ResC.Fail(coreModules.Msg);

        foreach(var module in coreModules.Value)
        {
            var subCommandProxy = new SubCommandModuleAttribute(module.ModuleCommands, module.ModuleDescription);
            list.Add((subCommandProxy, (x) => ExecuteModule(module, x)));
        }

        return ResC.Ok();
    }

    private Res ExecuteModule(ICoreCommandModule module, string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter($"Subcommand for {module.ModuleName} command");
        return module.Execute(args);
    }
}