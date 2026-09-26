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
        return _audioEngine is not null && !_audioEngine.IsDisposed 
            ? ResC.TOk(_audioEngine.CaptureDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve capture devices, audio engine not available", _logger);
    }

    public Res<AudioCaptureDevice>? CreateCaptureDevice()
    {
        var updateRes = UpdateDeviceList();
        if (!updateRes.IsOk) return ResC.TFail<AudioCaptureDevice>(updateRes.Msg);

        var deviceInfos = GetCaptureDevices();
        if (!deviceInfos.IsOk) return ResC.TFail<AudioCaptureDevice>(deviceInfos.Msg);

        var deviceInfo = FindDeviceWithChecks(deviceInfos.Value, _config.Recognition_MicrophoneName, "capture");
        if (deviceInfo is null) return null;
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
        }, $"Failed initializing capture device {deviceInfo.Value.Name}", _logger);
    }

    public Res<AudioCaptureDeviceProxy>? CreateCaptureDeviceProxy()
    {
        var dev = CreateCaptureDevice();
        if (dev is null) return null;
        return dev.IsOk
            ? ResC.TOk<AudioCaptureDeviceProxy>(new (dev.Value, _logger))
            : ResC.TFail<AudioCaptureDeviceProxy>(dev.Msg);
    }
    #endregion

    #region Playback
    public Res<DeviceInfo[]> GetPlaybackDevices()
    {
        return _audioEngine is not null && !_audioEngine.IsDisposed 
            ? ResC.TOk(_audioEngine.PlaybackDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve playback devices, audio engine not available", _logger);
    }

    public Res<AudioPlaybackDeviceProxy>? CreatePlaybackDeviceProxy(string name, ILogger deviceLogger)
    {
        var updateRes = UpdateDeviceList();
        if (!updateRes.IsOk) return ResC.TFail<AudioPlaybackDeviceProxy>(updateRes.Msg);

        var deviceInfos = GetPlaybackDevices();
        if (!deviceInfos.IsOk) return ResC.TFail<AudioPlaybackDeviceProxy>(deviceInfos.Msg);

        var deviceInfo = FindDeviceWithChecks(deviceInfos.Value, name, "playback");
        if (deviceInfo is null) return null;
        if (!deviceInfo.IsOk) return ResC.TFail<AudioPlaybackDeviceProxy>(deviceInfo.Msg);

        var format = new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        _logger.Debug("Creating playback device for device {devName}", deviceInfo.Value.Name);
        return ResC.TWrap(() =>
        {
            var device = _audioEngine!.InitializePlaybackDevice(deviceInfo.Value, format);
            _logger.Debug("Created playback device for device {devName}", deviceInfo.Value.Name);
            return ResC.TOk(new AudioPlaybackDeviceProxy(device, deviceLogger));
        }, $"Failed initializing playback device {deviceInfo.Value.Name}", _logger);
    }
    #endregion

    #region Util
    private Res<DeviceInfo>? FindDeviceWithChecks(DeviceInfo[] devices, string configId, string deviceTypeForLog) {
        if (_audioEngine is null || _audioEngine.IsDisposed)
            return ResC.TFailLog<DeviceInfo>($"Unable to retrieve {deviceTypeForLog} device, audio engine is not available", logger);
        
        var devInfo = AudioUtils.FindDevice(devices, configId, logger);
        if (!devInfo.HasValue)
        {
            _logger.Error("Unable to retrieve {deviceTypeForLog} device, none found", deviceTypeForLog);
        }

        return devInfo.HasValue ? ResC.TOk(devInfo.Value) : null;
    }

    public Res UpdateDeviceList()
    {
        if (_audioEngine is null || _audioEngine.IsDisposed)
            return ResC.FailLog("Audio devices could not be updated, engine is not available", _logger);

        return ResC.WrapR(_audioEngine.UpdateAudioDevicesInfo, "Failed to update audio devices", _logger);
    }
    #endregion
}