using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Input;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ExternalInputCommandModule))]
public class ExternalInputCommandModule(IExternalInputService input, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule
{
    private readonly IExternalInputService _input = input;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    [SubCommandModule(["send"], "Send a message")]
    public CommandResult CmdSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendMessage(args);
        Console.WriteLine($"Sent message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["notify"], "Send a notification")]
    public CommandResult CmdNotify(string? args)
    {
        if (OnEmpty(args: args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendNotification(args);
        Console.WriteLine($"Sent message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["flag-preprocessfull"], "Edit DoPreprocessFull Flag")]
    public CommandResult CmdFlagPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessFull));
    }

    [SubCommandModule(["flag-preprocesspartial"], "Edit DoPreprocessPartial Flag")]
    public CommandResult CmdFlagPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessPartial));
    }

    [SubCommandModule(["flag-translate"], "Edit DoTranslate Flag")]
    public CommandResult CmdFlagTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoTranslate));
    }

    [SubCommandModule(["flag-audio"], "Edit SendViaAudio Flag")]
    public CommandResult CmdFlagSendViaAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_SendViaAudio));
    }

    [SubCommandModule(["flag-other"], "Edit SendViaOther Flag")]
    public CommandResult CmdFlagSendViaOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_SendViaOther));
    }

    [SubCommandModule(["flag-text"], "Edit SendViaText Flag")]
    public CommandResult CmdFlagSendViaText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_SendViaText));
    }
}