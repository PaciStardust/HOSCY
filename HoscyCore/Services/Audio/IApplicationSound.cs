using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Audio;

public interface IApplicationSound : IService
{
    public void PlayMuteSound();
    public void PlayUnmuteSound();
    public void PlayNotificationSound();
    public Res Refresh();
    public bool IsRefreshNeeded();
}