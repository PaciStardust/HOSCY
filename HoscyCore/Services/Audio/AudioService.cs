using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
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
    public Res<DeviceInfo[]> GetCaptureInfos()
    {  
        return AudioUtils.GetCaptureInfosForEngine(_logger, _audioEngine);
    }

    public Res<IAudioCaptureDeviceProxy>? CreateCapture(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null)
    {
        return AudioUtils.CreateCaptureForEngine(_logger, devLogger, _audioEngine, primaryName, fallbackName, finalEmptyNotDefault, format);
    }
    #endregion

    #region Playback
    public Res<DeviceInfo[]> GetPlaybackInfos()
    {
        return AudioUtils.GetPlaybackInfosForEngine(_logger, _audioEngine);
    }

    public Res<IAudioPlaybackDeviceProxy>? CreatePlayback(ILogger devLogger, string primaryName, string fallbackName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null)
    {
        return AudioUtils.CreatePlaybackForEngine(_logger, devLogger, _audioEngine, primaryName, fallbackName, finalEmptyNotDefault, format);
    }
    #endregion

    #region Util
    public Res UpdateDeviceList()
    {
        return AudioUtils.UpdateDeviceListForEngine(_logger, _audioEngine);
    }
    #endregion
}