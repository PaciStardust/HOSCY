using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Voice.Core;

public interface IVoiceManagerService : ISoloModuleManager<IVoiceModuleStartInfo>
{
    public Res Enqueue(string text);
    public Res Clear();
    public void RefreshPlayback();
    public event Action<AudioPlaybackDeviceStatusInfo> OnPlaybackStatusChanged;
    public AudioPlaybackDeviceStatusInfo GetPlaybackStatus();
    public event Action<ServiceStatus> OnModuleStatusChanged;
}