using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;

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
    public CommandResult CmdDevices()
    {
        var mics = _audio.GetCaptureDevices();
        var speakers = _audio.GetPlaybackDevices();

        var micString = mics is null
            ? "[NOT LOADED]"
            : mics.Length == 0
                ? "[NONE]"
                : string.Join("\n", mics.Select(x => $" - {x.Id} | {x.Name}"));
        var speakerString = speakers is null
            ? "[NOT LOADED]"
            : speakers.Length == 0
                ? "[NONE]"
                : string.Join("\n", speakers.Select(x => $" - {x.Id} | {x.Name}"));

        Console.WriteLine($"Microphones:\n{micString}\n\nSpeakers:\n{speakerString}");
        return CommandResult.Success;
    }

    [SubCommandModule(["microphone-id"], "Set the microphone to use")]
    public CommandResult CmdMicrophone()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentMicrophoneId));
    }

    [SubCommandModule(["system-speaker-id"], "Set the speaker to use for system audio")]
    public CommandResult CmdSystemSpeaker()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentSpeakerSystemId));
    }

    [SubCommandModule(["system-output-id"], "Set the speaker to use for output audio")]
    public CommandResult CmdOutputSpeaker()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_CurrentSpeakerOutputId));
    }
}