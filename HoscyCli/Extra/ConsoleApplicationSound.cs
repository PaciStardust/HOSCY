using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Extra;

[PrototypeLoadIntoDiContainer(typeof(ConsoleApplicationSound))]
public class ConsoleApplicationSound : IApplicationSound
{
    public void PlayMuteSound()
    {
        Console.Beep();
    }

    public void PlayUnmuteSound()
    {
        Console.Beep();
    }

    public void PlayNotificationSound()
    {
        Console.Beep();
    }

    public Res Refresh()
    {
        return ResC.Ok();
    }

    public event Action<AudioPlaybackDeviceStatusInfo> OnPlaybackStatusChanged = delegate { };
    public AudioPlaybackDeviceStatusInfo GetPlaybackStatus()
    {
        return new(AudioPlaybackDeviceState.Running, "Console");
    }
}