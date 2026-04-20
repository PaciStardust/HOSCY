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
        return ResC.Ok();
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
        var refRes = UpdateAudioDevices();
        return refRes.IsOk
            ? ResC.TOk(_audioEngine!.CaptureDevices)
            : ResC.TFail<DeviceInfo[]>(refRes.Msg);
    }

    public Res<AudioCaptureDevice> CreateCaptureDevice()
    {
        var deviceInfos = GetCaptureDevices();
        if (!deviceInfos.IsOk) return ResC.TFail<AudioCaptureDevice>(deviceInfos.Msg);

        var deviceInfo = FindDeviceWithChecks(deviceInfos.Value, _config.Audio_CurrentMicrophoneName, "capture");
        if (!deviceInfo.IsOk) return ResC.TFail<AudioCaptureDevice>(deviceInfo.Msg);

        var format = new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        _logger.Debug("Creating capture device for device {devName}", deviceInfo.Value.Name);
        return ResC.TWrap(() =>
        {
            var device = _audioEngine!.InitializeCaptureDevice(deviceInfo.Value, format);
            _logger.Debug("Created capture device for device {devName}", deviceInfo.Value.Name);
            return ResC.TOk(device);
        }, $"Failed initializing audio device {deviceInfo.Value.Name}", _logger);
    }

    public Res<AudioCaptureDeviceProxy> CreateCaptureDeviceProxy()
    {
        var dev = CreateCaptureDevice();
        return dev.IsOk
            ? ResC.TOk<AudioCaptureDeviceProxy>(new (dev.Value, _logger))
            : ResC.TFail<AudioCaptureDeviceProxy>(dev.Msg);
    }
    #endregion

    #region Playback
    public Res<DeviceInfo[]> GetPlaybackDevices()
    {
        var refRes = UpdateAudioDevices();
        return refRes.IsOk
            ? ResC.TOk(_audioEngine!.PlaybackDevices)
            : ResC.TFail<DeviceInfo[]>(refRes.Msg);
    }
    #endregion

    #region Util
    private Res<DeviceInfo> FindDeviceWithChecks(DeviceInfo[] devices, string configId, string deviceTypeForLog) {
        if (_audioEngine is null || _audioEngine.IsDisposed)
            return ResC.TFailLog<DeviceInfo>($"Unable to retrieve {deviceTypeForLog} device, audio engine is not available", logger);
        
        var devInfo = AudioUtils.FindDevice(devices, configId, logger);
        return devInfo.HasValue
            ? ResC.TOk(devInfo.Value)
            : ResC.TFailLog<DeviceInfo>($"Unable to retrieve {deviceTypeForLog} device, none found", logger, lvl: ResMsgLvl.Error);
    }

    private Res UpdateAudioDevices()
    {
        if (_audioEngine is null || _audioEngine.IsDisposed)
            return ResC.FailLog("Audio devices could not be updated, engine is not available", _logger);

        return ResC.WrapR(_audioEngine.UpdateAudioDevicesInfo, "Failed to update audio devices", _logger);
    }
    #endregion
}