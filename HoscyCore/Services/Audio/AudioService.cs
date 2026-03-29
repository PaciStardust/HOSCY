using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
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

    protected override void StartForService()
    {
        _logger.Debug("Starting audio engine");
        _audioEngine = new MiniAudioEngine();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForService()
    {
        if (_audioEngine is not null && !_audioEngine.IsDisposed)
        {
            _audioEngine.Dispose();
        }
        _audioEngine = null;
    }
    #endregion

    #region Capture
    public DeviceInfo[]? GetCaptureDevices()
    {
        _audioEngine?.UpdateAudioDevicesInfo();
        return _audioEngine?.CaptureDevices;
    }

    public AudioCaptureDevice CreateCaptureDevice() //todo: [TEST] create test for this
    {
        var devInfo = FindDeviceWithChecks(GetCaptureDevices(), _config.Audio_CurrentMicrophoneName, "capture");

        var format = new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        _logger.Debug("Creating capture device for device {devName}", devInfo);
        return _audioEngine!.InitializeCaptureDevice(devInfo, format);
    }

    public AudioCaptureDeviceProxy CreateCaptureDeviceProxy()
    {
        return new(CreateCaptureDevice(), _logger);
    }
    #endregion

    #region Playback
    public DeviceInfo[]? GetPlaybackDevices()
    {
        _audioEngine?.UpdateAudioDevicesInfo();
        return _audioEngine?.PlaybackDevices;
    }
    #endregion

    #region Util
    private DeviceInfo FindDeviceWithChecks(DeviceInfo[]? devices, string configId, string deviceTypeForLog) {
        if (_audioEngine is null || _audioEngine.IsDisposed)
        {
            _logger.Warning("Unable to retrieve {deviceTypeForLog} device, audio engine is not available", deviceTypeForLog);
            throw new ArgumentNullException($"Unable to {deviceTypeForLog} capture device, audio engine is not available");
        }
        
        var devInfo = AudioUtils.FindDevice(devices, configId, logger);
        if (devInfo is null)
        {
            _logger.Warning("Unable to retrieve {deviceTypeForLog} device, none found", deviceTypeForLog);
            throw new ArgumentNullException($"Unable to retrieve {deviceTypeForLog} device, none found");
        }

        return devInfo.Value;
    }
    #endregion
}