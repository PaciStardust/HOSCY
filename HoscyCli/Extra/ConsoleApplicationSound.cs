using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Extra;

[PrototypeLoadIntoDiContainer(typeof(IApplicationSound))]
public class ConsoleApplicationSound : IApplicationSound
{
    public void PlayMuteSound()
    {
        Console.Beep();
    }
}