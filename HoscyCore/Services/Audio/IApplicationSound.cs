using HoscyCore.Services.Core;

namespace HoscyCore.Services.Audio;

public interface IApplicationSound : IService //todo: fix missing sound
{
    public void PlayMuteSound();
    public void PlayNotificationSound();
}