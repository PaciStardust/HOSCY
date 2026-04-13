using HoscyCore.Services.Core;
using HoscyCore.Utility;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public Res<DeviceInfo[]> GetCaptureDevices();
    public Res<AudioCaptureDeviceProxy> CreateCaptureDeviceProxy();
    public Res<AudioCaptureDevice> CreateCaptureDevice();
    
    public Res<DeviceInfo[]> GetPlaybackDevices();
}