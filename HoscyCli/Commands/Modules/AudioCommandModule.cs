using HoscyCli.Commands.Core;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AudioCommandModule))]
public class AudioCommandModule(IAudioService audio) : AttributeCommandModule, ICoreCommandModule
{
    private readonly IAudioService _audio = audio;

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
}