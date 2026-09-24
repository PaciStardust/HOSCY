using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Audio;

public class SwappableAudioPlaybackDevice
(
    ILogger logger,
    IAudioService audio
)
    : IDisposable
{
    #region Injects
    private readonly ILogger _logger = logger;
    private readonly IAudioService _audio = audio;
    #endregion

    #region Vars
    private AudioPlaybackDeviceProxy? _playback;
    private CancellationTokenSource _playbackCancellation = new();
    private volatile bool _deviceInUse = false;
    #endregion

    #region Utils
    public bool IsPlaybackRunning => _playback?.IsRunning ?? false;
    public void Dispose()
    {
        _playbackCancellation?.Dispose();

        _playback?.Dispose();
        _playback = null;
    }
    #endregion

    #region Playback
    public async Task<Res> PlayAsync(float volume, Func<MemoryStream, CancellationToken, Task<Res>> writerTask)
    {
        var res = await ResC.WrapAsync(PlayAsyncInternal(volume, writerTask),
            "Failed to play audio", _logger);
        _deviceInUse = false;
        return res;
    }

    private async Task<Res> PlayAsyncInternal(float volume, Func<MemoryStream, CancellationToken, Task<Res>> writerTask)
    {
        if (_playback is null || !_playback.IsRunning)
        {
            _deviceInUse = false;
            return ResC.FailLog("Unable to play audio, the device is not initialized", _logger);
        }
        _deviceInUse = true;
        _playback.ClearStream();

        if (_playbackCancellation.IsCancellationRequested)
        {
            _logger.Verbose("Previous CT is cancelled, recreating");
            _playbackCancellation.Dispose();
            _playbackCancellation = new();
        }

        _logger.Verbose("Calling writer task");
        var writerRes = await writerTask(_playback.Stream, _playbackCancellation.Token);
        if (!writerRes.IsOk)
        {
            _playback.ClearStream();
            _deviceInUse = false;
            return _playbackCancellation.IsCancellationRequested ? ResC.Ok() : writerRes;
        }

        _logger.Verbose("Playing sound");
        var playbackRes = await _playback.PlayAsync(volume, _playbackCancellation.Token);
        _playback.ClearStream();
        _deviceInUse = false;
        return !writerRes.IsOk && !_playbackCancellation.IsCancellationRequested ? playbackRes : ResC.Ok();
    }

    public Res CancelCurrentAudio()
    {
        if (!_deviceInUse) return ResC.Ok();

        _logger.Debug("Cancelling current audio");
        _playbackCancellation.Cancel();
        if (!OtherUtils.WaitWhile(() => _deviceInUse, 20_000, 10))
        {
            _logger.Error("Failed to cancel audio, playback will be stopped");
            _deviceInUse = false;
            return ClearPlayback();
        }
        return ResC.Ok();
    }
    #endregion

    #region Playback Setting
    public void ForcePlaybackReload()
    {
        _forcePlaybackReload = true;
    }
    private bool _forcePlaybackReload = false;
    private string _lastErrorPlaybackName = string.Empty;
    public Res SwapPlaybackIfNeeded(string devName)
    {
        if (string.IsNullOrEmpty(devName)) return ResC.Ok();
        if (!_forcePlaybackReload)
        {
            var curDevName = _playback?.GetDeviceName();
            if (curDevName is not null && curDevName == devName) return ResC.Ok();
            if (devName == _lastErrorPlaybackName) return ResC.Ok();
        }

        var res = ClearPlayback(); 
        if (!res.IsOk) return res;

        res = CreatePlayback(devName);
        if (!res.IsOk)
        {
            _lastErrorPlaybackName = devName;
        }
        else
        {
            _lastErrorPlaybackName = string.Empty;
        }
        return res;
    }

    private Res CreatePlayback(string devName)
    {
        _logger.Debug("Creating new playback");

        if (_playback is not null)
            return ResC.FailLog("Unable to create playback, it already exists", _logger);

        var playback = _audio.CreatePlaybackDeviceProxy(devName, _logger);
        if (playback is null)
        {
            return ResC.FailLog("No microphone could be located, no voice output will be possible", _logger, lvl: ResMsgLvl.Warning);
        }
        if (!playback.IsOk) return ResC.Fail(playback.Msg);
        _playback = playback.Value;

        var playbackOn = _playback.Start();
        if (!playbackOn.IsOk) return playbackOn;

        return ResC.Ok();
    }

    private Res ClearPlayback()
    {
        if (_deviceInUse)
        {
            return ResC.FailLog("Unable to clear playback while in use", _logger);
        }

        var res = _playback?.Stop() ?? ResC.Ok();
        _playback?.Dispose();
        _playback = null;

        return res;
    }
    #endregion
}