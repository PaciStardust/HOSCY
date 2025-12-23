using HoscyCore.Services.DependencyCore;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;

namespace HoscyCore.Services.Audio;

[PrototypeLoadIntoDiContainer(typeof(IAudioService), Lifetime.Singleton)]
public class AudioService(ILogger logger) : StartStopServiceBase, IAudioService
{
    private AudioEngine? _audioEngine;
    private readonly ILogger _logger = logger.ForContext<AudioService>();

    public override bool IsRunning()
    {
        return _audioEngine is not null;
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    public override void Stop()
    {
        LogStopBegin(GetType(), _logger);
        if (_audioEngine?.IsDisposed ?? false)
        {
            _audioEngine.Dispose();
        }
        _audioEngine = null;
        LogStopComplete(GetType(), _logger);
    }

    protected override void StartInternal()
    {
        LogStartBegin(GetType(), _logger);
        if (IsRunning())
        {
            LogStartAlreadyRunning(GetType(), _logger);
            return;
        }
        _audioEngine = new MiniAudioEngine();
        LogStartComplete(GetType(), _logger);
    }

    public SoundFlow.Structs.DeviceInfo[]? GetCaptureDevices()
    {
        _audioEngine?.UpdateDevicesInfo();
        return _audioEngine?.CaptureDevices;
    }

    public SoundFlow.Structs.DeviceInfo[]? GetPlaybackDevices()
    {
        _audioEngine?.UpdateDevicesInfo();
        return _audioEngine?.PlaybackDevices;
    }
}