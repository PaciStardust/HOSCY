using HoscyCore.Utility;

namespace HoscyCore.Services.Audio;

public interface IAudioPlaybackDeviceProxy : IDisposable
{
    public MemoryStream Stream { get; }

    public AudioPlaybackDeviceStatusInfo GetDeviceStatus();
    public Res Start();
    public Res Stop();
    public Task<Res> PlayAsync(float volume, CancellationToken ct);
    public void ClearStream();
}