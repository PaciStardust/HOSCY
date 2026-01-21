using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Input;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ExternalInputCommandModule))]
public class ExternalInputCommandModule(IExternalInputService input, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule
{
    private readonly IExternalInputService _input = input;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    [SubCommandModule(["send"], "Send a message")] //todo: [FEAT] Notify
    public CommandResult CmdSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendMessage(args);
        Console.WriteLine($"Sent message: {args}");
        return CommandResult.Success;   
    }
}