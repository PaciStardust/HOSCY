using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public Res<DeviceInfo[]> GetCaptureDevices();
    public Res<IAudioCaptureDeviceProxy>? CreateCaptureDeviceProxy();
    public Res<AudioCaptureDevice>? CreateCaptureDevice();
    
    public Res<DeviceInfo[]> GetPlaybackDevices();
    public Res<IAudioPlaybackDeviceProxy>? CreatePlaybackDeviceProxy(string name, ILogger deviceLogger, AudioFormat? format = null);

    public Res UpdateDeviceList();
}