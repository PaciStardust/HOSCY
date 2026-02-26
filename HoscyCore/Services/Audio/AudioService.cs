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
        _audioEngine?.UpdateDevicesInfo();
        return _audioEngine?.CaptureDevices;
    }

    public AudioCaptureDevice CreateCaptureDevice() //todo: [TEST] create test for this
    {
        var devInfo = FindDeviceWithChecks(GetCaptureDevices(), _config.Audio_CurrentMicrophoneId, "capture");

        var format = new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        _logger.Debug("Creating capture device for device {devName}", devInfo);
        return _audioEngine!.InitializeCaptureDevice(devInfo, format);
    }
    #endregion

    #region Playback
    public DeviceInfo[]? GetPlaybackDevices()
    {
        _audioEngine?.UpdateDevicesInfo();
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
        
        var devInfo = FindDevice(devices, _config.Audio_CurrentMicrophoneId);
        if (devInfo is null)
        {
            _logger.Warning("Unable to retrieve {deviceTypeForLog} device, none found", deviceTypeForLog);
            throw new ArgumentNullException($"Unable to retrieve {deviceTypeForLog} device, none found");
        }

        return devInfo.Value;
    }

    private DeviceInfo? FindDevice(DeviceInfo[]? devices, string configId)
    {
        if (devices is null || devices.Length == 0)
        {
            _logger.Warning("No audio devices provided for search, returning null");
            return null;
        }

        var configMatches = devices.Where(x => x.Id.ToString() == _config.Audio_CurrentMicrophoneId).ToArray();
        
        if (configMatches.Length == 0)
        {
            _logger.Warning("No audio device found for configuired id {configId}, picking default instead", configId);
        } 
        else 
        {
            if (configMatches.Length > 1)
            {
                _logger.Warning("More than one audio device found for id {configId}, picking first", configId);
            }
            return configMatches[0];
        }

        var defaultMatches = devices.Where(x => x.IsDefault).ToArray();

        if (defaultMatches.Length == 0)
        {
            _logger.Warning("No default audio device found, picking first instead");
            return devices[0];
        } 
        else 
        {
            if (defaultMatches.Length > 1)
            {
                _logger.Warning("More than one default audo device found, picking first");
            }
            return defaultMatches[0];
        }
    }
    #endregion
}