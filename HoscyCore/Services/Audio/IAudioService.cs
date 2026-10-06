using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public Res<DeviceInfo[]> GetCaptureInfos();
    public Res<IAudioCaptureDeviceProxy>? CreateCapture(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null);
    
    public Res<DeviceInfo[]> GetPlaybackInfos();
    public Res<IAudioPlaybackDeviceProxy>? CreatePlayback(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null);

    public Res UpdateDeviceList();
}