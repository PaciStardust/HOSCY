using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AudioCommandModule))]
public class AudioCommandModule(IAudioService audio, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly IAudioService _audio = audio;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Audio";
    public string ModuleDescription => "Configure audio devices";
    public string[] ModuleCommands => ["audio"];

    [SubCommandModule(["devices"], "List all devices")]
    public Res CmdDevices()
    {
        var micResult = _audio.GetCaptureDevices();
        if (!micResult.IsOk) return ResC.Fail(micResult.Msg);

        var speakerResult = _audio.GetPlaybackDevices();
        if (!speakerResult.IsOk) return ResC.Fail(speakerResult.Msg);

        var micString = micResult is null
            ? "[NOT LOADED]"
            : micResult.Value.Length == 0
                ? "[NONE]"
                : string.Join("\n", micResult.Value.Select(x => $" - {x.Name}"));
        var speakerString = speakerResult is null
            ? "[NOT LOADED]"
            : speakerResult.Value.Length == 0
                ? "[NONE]"
                : string.Join("\n", speakerResult.Value.Select(x => $" - {x.Name}"));

        Console.WriteLine($"Microphones:\n{micString}\n\nSpeakers:\n{speakerString}");
        return ResC.Ok();
    }

    [SubCommandModule(["microphone-id"], "Set the microphone to use")]
    public Res CmdMicrophone()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentMicrophoneName));
    }

    [SubCommandModule(["system-speaker-id"], "Set the speaker to use for system audio")]
    public Res CmdSystemSpeaker()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentSpeakerSystemName));
    }

    [SubCommandModule(["system-output-id"], "Set the speaker to use for output audio")]
    public Res CmdOutputSpeaker()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentSpeakerOutputName));
    }
}