using HoscyCore.Services.Core;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public SoundFlow.Structs.DeviceInfo[]? GetCaptureDevices();
    public SoundFlow.Structs.DeviceInfo[]? GetPlaybackDevices();
}