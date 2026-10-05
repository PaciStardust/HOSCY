using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

[PrototypeLoadIntoDiContainer(typeof(IAudioService), Lifetime.Singleton)]
public class AudioService(ILogger logger, ConfigModel config)
    : StartStopServiceBase(logger.ForContext<AudioService>()), IAudioService
{
    #region Vars
    private AudioEngine? _audioEngine;
    private readonly ConfigModel _config = config;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _audioEngine is not null;
    protected override bool IsProcessing()
        => IsStarted();

    protected override Res StartForService()
    {
        _logger.Debug("Starting audio engine");
        try
        {
            _audioEngine = new MiniAudioEngine();
        }
        catch (Exception ex)
        {
            return ResC.FailLog("Failed initializing audio engine", _logger, ex);
        }
        return UpdateDeviceList();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService()
    {
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        if (_audioEngine is not null && !_audioEngine.IsDisposed)
        {
            _audioEngine.Dispose();
        }
        _audioEngine = null;
    }
    #endregion

    #region Capture
    public Res<DeviceInfo[]> GetCaptureDevices()
    {  
        return AudioUtils.GetCaptureDevicesForEngine(_logger, _audioEngine);
    }

    public Res<AudioCaptureDevice>? CreateCaptureDevice()
    {
        return AudioUtils.CreateCaptureDeviceForEngine(_logger, _audioEngine, _config.Recognition_MicrophoneName);
    }

    public Res<IAudioCaptureDeviceProxy>? CreateCaptureDeviceProxy()
    {
        return AudioUtils.CreateCaptureDeviceProxyForEngine(_logger, _audioEngine, _config.Recognition_MicrophoneName);
    }
    #endregion

    #region Playback
    public Res<DeviceInfo[]> GetPlaybackDevices()
    {
        return AudioUtils.GetPlaybackDevicesForEngine(_logger, _audioEngine);
    }

    public Res<IAudioPlaybackDeviceProxy>? CreatePlaybackDeviceProxy(string name, ILogger deviceLogger, AudioFormat? format = null)
    {
        return AudioUtils.CreatePlaybackDeviceProxyForEngine(_logger, _audioEngine, name, format);
    }
    #endregion

    #region Util
    public Res UpdateDeviceList()
    {
        return AudioUtils.UpdateDeviceListForEngine(_logger, _audioEngine);
    }
    #endregion
}