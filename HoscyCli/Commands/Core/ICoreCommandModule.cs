namespace HoscyCli.Commands.Core;

public interface ICoreCommandModule : ICommandModule
{
    public string ModuleName { get; }
    public string ModuleDescription { get; }
    public string[] ModuleCommands { get; }
}