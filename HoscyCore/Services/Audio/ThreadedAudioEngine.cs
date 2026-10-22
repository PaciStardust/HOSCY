using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Interfaces;
using SoundFlow.Metadata.Models;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public class ThreadedAudioEngine(AudioEngine engine, ILogger logger, string id) : IDisposable
{
    private readonly AudioEngine _engine = engine;
    private readonly ILogger _logger = logger.ForContext<ThreadedAudioEngine>();
    private readonly string _id = id;
    private readonly Lock _lock = new();

    public AudioEngine Engine => _engine;
    public Lock Lock => _lock;

    public void Dispose()
    {
        lock(_lock)
        {
            _logger.Debug("{id}: Disposing threaded Audio Engine", _id);
            if (!_engine.IsDisposed)
            {
                _engine.Dispose();
            }
        }
    }

    public Res UpdateAudioDevicesInfo()
    {
        lock(_lock)
        {
            _logger.Verbose("{id}: Updating audio device list", _id);
            return ResC.WrapR(_engine.UpdateAudioDevicesInfo, $"{_id}: Failed to update Audio Devices", _logger);
        }
    }

    public DeviceInfo[] CaptureDevices => _engine.CaptureDevices;
    public DeviceInfo[] PlaybackDevices => _engine.PlaybackDevices;

    public Res<AudioCaptureDevice> InitializeCaptureDevice(DeviceInfo deviceInfo, AudioFormat format)
    {
        lock(_lock)
        {
            _logger.Verbose("{id}: Init capture device with name \"{deviceInfoName}\"", _id, deviceInfo.Name);
            return ResC.TWrapR(() => _engine.InitializeCaptureDevice(deviceInfo, format),
                $"{_id}: Failed to init capture device with name \"{deviceInfo.Name}\"", _logger);
        }
    }

    public Res<AudioPlaybackDevice> InitializePlaybackDevice(DeviceInfo deviceInfo, AudioFormat format)
    {
        lock(_lock)
        {
            _logger.Verbose("{id}: Init playback device with name \"{deviceInfoName}\"", _id, deviceInfo.Name);
            return ResC.TWrapR(() => _engine.InitializePlaybackDevice(deviceInfo, format),
                $"{_id}: Failed to init playback device with name \"{deviceInfo.Name}\"", _logger);
        }
    }

    public ISoundDecoder CreateDecoder(Stream stream, string formatInfo, AudioFormat format)
    {
        lock(_lock)
        {
            _logger.Verbose("{id}: Creating Decoder", _id);
            return _engine.CreateDecoder(stream, formatInfo, format);
        }
    }
    public ISoundDecoder CreateDecoder(Stream stream, out AudioFormat format, AudioFormat? hintFormat = null)
    {
        lock(_lock)
        {
            _logger.Verbose("{id}: Creating Decoder", _id);
            return _engine.CreateDecoder(stream, out format, hintFormat);
        }
    }
}