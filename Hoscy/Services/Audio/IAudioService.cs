using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public SoundFlow.Structs.DeviceInfo[]? GetCaptureDevices();
    public SoundFlow.Structs.DeviceInfo[]? GetPlaybackDevices();
}