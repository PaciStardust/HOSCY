using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public interface IAudioService : IAutoStartStopService
{
    public Res<DeviceInfo[]> GetCaptureInfos();
    public Res<DeviceInfo>? GetCaptureInfoForNames(string primaryName, string fallbackName = "");
    public Res<IAudioCaptureDeviceProxy>? CreateCapture(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null);
    
    public Res<DeviceInfo[]> GetPlaybackInfos();
    public Res<DeviceInfo>? GetPlaybackInfoForNames(string primaryName, string fallbackName = "");
    public Res<IAudioPlaybackDeviceProxy>? CreatePlayback(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null);

    public Res UpdateDeviceList();
}