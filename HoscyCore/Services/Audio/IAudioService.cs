using HoscyCore.Services.Core;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public DeviceInfo[]? GetCaptureDevices();
    public AudioCaptureDeviceProxy CreateCaptureDevice();
    
    public DeviceInfo[]? GetPlaybackDevices();
}