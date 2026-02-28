using HoscyCore.Services.Core;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public DeviceInfo[]? GetCaptureDevices();
    public AudioCaptureDeviceProxy CreateCaptureDeviceProxy();
    public AudioCaptureDevice CreateCaptureDevice();
    
    public DeviceInfo[]? GetPlaybackDevices();
}