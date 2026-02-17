using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Misc;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(InputCommandModule))]
public class InputCommandModule(IInputService input, ReflectPropEditCommandModule reflectCm, ConfigModel config) : AttributeCommandModule, ICoreCommandModule
{
    private readonly IInputService _input = input;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly ConfigModel _config = config;

    public string ModuleName => "Input";
    public string ModuleDescription => "Configure and send manual/external input";
    public string[] ModuleCommands => ["input"];

    #region External
    [SubCommandModule(["e-t-send"], "Send an external text message")]
    public CommandResult CmdExTextSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendExternalTextMessage(args);
        Console.WriteLine($"Sent external text message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["e-a-send"], "Send an external audio message")]
    public CommandResult CmdExAudioSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendExternalAudioMessage(args);
        Console.WriteLine($"Sent external audio message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["e-o-send"], "Send an external other message")]
    public CommandResult CmdExOtherSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendExternalOtherMessage(args);
        Console.WriteLine($"Sent external other message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["e-t-notify"], "Send an external notification")]
    public CommandResult CmdExTextNotify(string? args)
    {
        if (OnEmpty(args: args, message: "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendExternalTextNotification(args);
        Console.WriteLine($"Sent external text notification: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["e-preprocess-full"], "Edit external DoPreprocessFull Flag")]
    public CommandResult CmdExPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessFull));
    }

    [SubCommandModule(["e-preprocess-partial"], "Edit external DoPreprocessPartial Flag")]
    public CommandResult CmdExPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessPartial));
    }

    [SubCommandModule(["e-translate"], "Edit external DoTranslate Flag")]
    public CommandResult CmdExTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoTranslate));
    }
    #endregion

    #region External
    [SubCommandModule(["m-send"], "Send an manual message")]
    public CommandResult CmdMaSend(string? args)
    {
        if (OnEmpty(args, "Must provide contents to send")) return CommandResult.MissingParameter;
        _input.SendManualMessage(args);
        Console.WriteLine($"Sent manual message: {args}");
        return CommandResult.Success;   
    }

    [SubCommandModule(["m-preprocess-full"], "Edit manual DoPreprocessFull Flag")]
    public CommandResult CmdMaPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoPreprocessFull));
    }

    [SubCommandModule(["m-preprocess-partial"], "Edit manual DoPreprocessPartial Flag")]
    public CommandResult CmdMaPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoPreprocessPartial));
    }

    [SubCommandModule(["m-translate"], "Edit manual DoTranslate Flag")]
    public CommandResult CmdMaTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoTranslate));
    }

    [SubCommandModule(["m-audio"], "Edit manual SendViaAudio Flag")]
    public CommandResult CmdMaSendViaAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaAudio));
    }

    [SubCommandModule(["m-other"], "Edit manual SendViaOther Flag")]
    public CommandResult CmdMaSendViaOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaOther));
    }

    [SubCommandModule(["m-text"], "Edit manual SendViaText Flag")]
    public CommandResult CmdMaSendViaText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaText));
    }

    [SubCommandModule(["m-p-edit", "m-p-list"], "Edit manual presets")]
    public CommandResult CmdMaPresetEdit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_TextPresets));
    }

    [SubCommandModule(["m-p-send"], "Send a manual preset")] 
    public CommandResult CmdMaPresetSend(string? preset)
    {
        var presets = _config.ManualInput_TextPresets;
        if (presets.Count == 0)
        {
            Console.WriteLine("No presets were found");
            return CommandResult.Success;
        }

        if (string.IsNullOrWhiteSpace(preset))
        {
            Console.WriteLine($"All presets: {string.Join("\n", presets.Select(x => $" - {x.Key} : {x.Value}"))}");
            return CommandResult.NotFound;
        }
        
        var match = presets.TryGetValue(preset, out var val);
        if (OnFalse(match, "Unable to locate preset with specified name"))
            return CommandResult.NotFound;

        return CmdMaSend(val);
    }
    #endregion
}