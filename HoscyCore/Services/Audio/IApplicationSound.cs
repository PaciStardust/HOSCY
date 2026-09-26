using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Audio;

public interface IApplicationSound : IService //todo: fix missing sound
{
    public void PlayMuteSound();
    public void PlayNotificationSound();
    public Res Refresh();
}