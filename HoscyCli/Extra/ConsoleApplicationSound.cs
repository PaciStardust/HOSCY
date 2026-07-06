using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Extra;

[PrototypeLoadIntoDiContainer(typeof(ConsoleApplicationSound))]
public class ConsoleApplicationSound : IApplicationSound
{
    public void PlayMuteSound()
    {
        Console.Beep();
    }

    public void PlayNotificationSound()
    {
        Console.Beep();
    }
}