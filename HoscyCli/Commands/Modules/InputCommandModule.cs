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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["e-preprocess-full"], nameof(ConfigModel.ExternalInput_DoPreprocessFull), ConfigModel.DESC_ExternalInput_DoPreprocessFull),
            new(["e-preprocess-partial"], nameof(ConfigModel.ExternalInput_DoPreprocessPartial), ConfigModel.DESC_ExternalInput_DoPreprocessPartial),
            new(["e-translate"], nameof(ConfigModel.ExternalInput_DoTranslate), ConfigModel.DESC_ExternalInput_DoTranslate),

            new(["m-preprocess-full"], nameof(ConfigModel.ManualInput_DoPreprocessFull), ConfigModel.DESC_ManualInput_DoPreprocessFull),
            new(["m-preprocess-partial"], nameof(ConfigModel.ManualInput_DoPreprocessPartial), ConfigModel.DESC_ManualInput_DoPreprocessPartial),
            new(["m-translate"], nameof(ConfigModel.ManualInput_DoTranslate), ConfigModel.DESC_ManualInput_DoTranslate),
            new(["m-audio"], nameof(ConfigModel.ManualInput_SendViaAudio), ConfigModel.DESC_ManualInput_SendViaAudio),
            new(["m-other"], nameof(ConfigModel.ManualInput_SendViaOther), ConfigModel.DESC_ManualInput_SendViaOther),
            new(["m-text"], nameof(ConfigModel.ManualInput_SendViaText), ConfigModel.DESC_ManualInput_SendViaText),
            new(["m-p-edit", "m-p-list"], nameof(ConfigModel.ManualInput_TextPresets), ConfigModel.DESC_ManualInput_TextPresets)
        ];
    }

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