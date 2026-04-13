using HoscyCore.Utility;

namespace HoscyCli.Commands.Core;

public interface ICommandModule
{
    public Res Execute(string command);
    public Res Execute(string command, string? parameters);
}