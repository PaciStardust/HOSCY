using HoscyCore.Utility;

namespace HoscyCli.Commands.Core;

public static class CResH
{
    public static Res MissingParameter(string name)
        => ResC.Fail(ResMsg.Inf($"The following parameter is missing: {name}"));

    public static Res NotFound(string name)
        => ResC.Fail(ResMsg.Inf($"{name} was not found"));

    public static void Print(string message, ResMsg msg)
        => Console.WriteLine($"{message}: {msg}");
}