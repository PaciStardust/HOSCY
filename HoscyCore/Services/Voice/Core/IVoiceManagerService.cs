using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Voice.Core;

public interface IVoiceManagerService : ISoloModuleManager<IVoiceModuleStartInfo>
{
    public Res Enqueue(string text);
    public Res Clear();
    public void RefreshPlayback();
    public bool IsPlaybackRefreshNeeded();
    public string? GetPlaybackName();
    public event Action<ServiceStatus> OnModuleStatusChanged;
}