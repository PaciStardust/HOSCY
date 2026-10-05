using HoscyCore.Utility;

namespace HoscyCore.Services.Audio;

public interface IAudioPlaybackDeviceProxy : IDisposable
{
    public MemoryStream Stream { get; }

    public string? GetDeviceName();
    public Res Start();
    public Res Stop();
    public bool IsRunning { get; }
    public Task<Res> PlayAsync(float volume, CancellationToken ct);
    public void ClearStream();
}