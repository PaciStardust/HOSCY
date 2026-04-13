using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Input;
using HoscyCore.Utility;

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
    [SubCommandModule(["e-send-t"], "Send an external text message")]
    public Res CmdExSendText(string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter("Text Message");
        _input.SendExternalTextMessage(args);
        Console.WriteLine($"Sent external text message: {args}");
        return ResC.Ok();   
    }

    [SubCommandModule(["e-send-a"], "Send an external audio message")]
    public Res CmdExSendAudio(string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter("Audio Message");
        _input.SendExternalAudioMessage(args);
        Console.WriteLine($"Sent external audio message: {args}");
        return ResC.Ok();   
    }

    [SubCommandModule(["e-send-o"], "Send an external other message")]
    public Res CmdExSendOther(string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter("Other Message");
        _input.SendExternalOtherMessage(args);
        Console.WriteLine($"Sent external other message: {args}");
        return ResC.Ok();   
    }

    [SubCommandModule(["e-sent-notify-t"], "Send an external notification")]
    public Res CmdExSendNotifyText(string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter("External Notification");
        _input.SendExternalTextNotification(args);
        Console.WriteLine($"Sent external text notification: {args}");
        return ResC.Ok();   
    }

    [SubCommandModule(["e-preprocess-full"], "Edit external DoPreprocessFull Flag")]
    public Res CmdExPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessFull));
    }

    [SubCommandModule(["e-preprocess-partial"], "Edit external DoPreprocessPartial Flag")]
    public Res CmdExPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoPreprocessPartial));
    }

    [SubCommandModule(["e-translate"], "Edit external DoTranslate Flag")]
    public Res CmdExTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ExternalInput_DoTranslate));
    }
    #endregion

    #region External
    [SubCommandModule(["m-send"], "Send an manual message")]
    public Res CmdMaSend(string? args)
    {
        if (OnEmpty(args)) return CResH.MissingParameter("Manual Message");
        _input.SendManualMessage(args);
        Console.WriteLine($"Sent manual message: {args}");
        return ResC.Ok();   
    }

    [SubCommandModule(["m-preprocess-full"], "Edit manual DoPreprocessFull Flag")]
    public Res CmdMaPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoPreprocessFull));
    }

    [SubCommandModule(["m-preprocess-partial"], "Edit manual DoPreprocessPartial Flag")]
    public Res CmdMaPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoPreprocessPartial));
    }

    [SubCommandModule(["m-translate"], "Edit manual DoTranslate Flag")]
    public Res CmdMaTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_DoTranslate));
    }

    [SubCommandModule(["m-audio"], "Edit manual SendViaAudio Flag")]
    public Res CmdMaSendViaAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaAudio));
    }

    [SubCommandModule(["m-other"], "Edit manual SendViaOther Flag")]
    public Res CmdMaSendViaOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaOther));
    }

    [SubCommandModule(["m-text"], "Edit manual SendViaText Flag")]
    public Res CmdMaSendViaText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_SendViaText));
    }

    [SubCommandModule(["m-p-edit", "m-p-list"], "Edit manual presets")]
    public Res CmdMaPresetEdit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ManualInput_TextPresets));
    }

    [SubCommandModule(["m-p-send"], "Send a manual preset")] 
    public Res CmdMaPresetSend(string? preset)
    {
        var presets = _config.ManualInput_TextPresets;
        if (presets.Count == 0)
        {
            Console.WriteLine("No presets were found");
            return ResC.Ok();
        }

        if (OnEmpty(preset))
        {
            Console.WriteLine($"All presets: {string.Join("\n", presets.Select(x => $" - {x.Key} : {x.Value}"))}");
            return CResH.NotFound("Preset");
        }
        
        var match = presets.TryGetValue(preset, out var val);
        if (!match)
            return CResH.NotFound($"Preset with name \"{preset}\"");

        return CmdMaSend(val);
    }
    #endregion
}