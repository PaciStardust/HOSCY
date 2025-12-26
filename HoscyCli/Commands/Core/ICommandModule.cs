namespace HoscyCli.Commands.Core;

public interface ICommandModule
{
    public CommandResult Execute(string command);
    public CommandResult Execute(string command, string? parameters);
}