using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HoscyAvaloniaUi.Utility;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(IApplicationSound))]
public class AvaloniaApplicationSound : StartStopServiceBase, IApplicationSound, IAutoStartStopService //todo: IN/Out at same time breaks, add warning
{
    #region Injects
    private readonly ConfigModel _config;
    private readonly IAudioService _audio;
    private readonly IBackToFrontNotifyService _notify;

    public AvaloniaApplicationSound(ILogger logger, ConfigModel config, IAudioService audio, IBackToFrontNotifyService notify) 
        : base(logger.ForContext<AvaloniaApplicationSound>())
    {
        _config = config;
        _audio = audio;
        _notify = notify;

        _playback = new(_logger, _audio);
    }
    #endregion

    #region Vars
    private SwappableAudioPlaybackDevice<string> _playback;
    private Task? _processingTask = null;
    private volatile bool _shouldTaskRun = true;
    private string _nextToProcess = string.Empty;
    private readonly Lock _nextToProcessLock = new();
    private volatile bool _deviceReloadNeeded = true;
    #endregion

    #region Startup
    protected override bool IsStarted()
        => _processingTask is not null;
    protected override bool IsProcessing()
        => IsStarted() && _playback.IsPlaybackRunning;
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StartForService()
    {
        _playback.Dispose();
        _playback = new(_logger, _audio);

        _nextToProcess = string.Empty;
        _deviceReloadNeeded = true;
        _shouldTaskRun = true;
        _processingTask = Task.Run(RunProcessingLoop);
        return ResC.Ok();
    }

    protected override Res StopForService()
    {
        List<ResMsg> messages = [];

        _shouldTaskRun = false;
        _playback.CancelCurrentAudio().IfFail(messages.Add);

        LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_processingTask, 500,
            new StartStopServiceException("Unable to stop processing loop"), _logger)
            .IfFail(messages.Add);

        _playback.ClearPlayback().IfFail(messages.Add);

        _nextToProcess = string.Empty;
        _deviceReloadNeeded = false;

        return messages.Count == 0 ? ResC.Ok() : ResC.FailM(messages);
    }

    protected override void DisposeCleanup()
    {
        _processingTask?.Dispose();
        _processingTask = null;

        _playback.Dispose();
    }
    #endregion

    #region Playback
    public Res Refresh()
    {
        _logger.Debug("Set flag to refresh audio device");
        _deviceReloadNeeded = true;
        return ResC.Ok();
    }

    private async Task RunProcessingLoop()
    {
        while (_shouldTaskRun)
        {
            if (_deviceReloadNeeded)
            {
                _deviceReloadNeeded = false;
                var swapRes = _playback.SwapPlayback(_config.Debug_InfoNoiseSpeakerName);
                if (!swapRes.IsOk)
                {
                    SetFaultLogNotify(swapRes.Msg, "Failed to load speaker for system audio", _notify, _logger);
                    lock(_nextToProcessLock)
                    {
                        _nextToProcess = string.Empty;
                    }
                }
            }

            _nextToProcessLock.Enter();
            if (_nextToProcess.Length == 0)
            {
                _nextToProcessLock.Exit();
                await Task.Delay(25);
                continue;
            }

            var toProcess = _nextToProcess;
            _nextToProcess = string.Empty;
            _nextToProcessLock.Exit();

            var playbackRes = await _playback.PlayAsync(_config.Debug_InfoNoiseVolumePercent, toProcess, WriteNextToProcess);
            if (!playbackRes.IsOk)
            {
                SetFaultLogNotify(playbackRes.Msg, "Failed to play audio", _notify, _logger);
                await Task.Delay(10_000);
            }
        }
    }
    private Task<Res> WriteNextToProcess(MemoryStream stream, CancellationToken _, string toProcess)
    {
        var streamRes = AvaloniaUtil.GetAvaloniaResource(toProcess, _logger);
        if (!streamRes.IsOk) return Task.FromResult(ResC.Fail(streamRes.Msg));
        var sourceStream = streamRes.Value;
        sourceStream.Position = 0;
        sourceStream.CopyTo(stream);
        return Task.FromResult(ResC.Ok());
    }
    #endregion

    #region Writing
    private void SetNextAudio(string resource)
    {
        var res = _playback.CancelCurrentAudio();
        if (!res.IsOk)
        {
            SetFaultLogNotify(res.Msg, "Failed to cancel last audio", _notify, _logger);
            return;
        }
        lock(_nextToProcessLock)
        {
            _nextToProcess = resource;
        }
    }

    public void PlayMuteSound()
    {
        SetNextAudio("avares://HoscyAvaloniaUi/Assets/Mute.wav");
    }

    public void PlayUnmuteSound()
    {
        SetNextAudio("avares://HoscyAvaloniaUi/Assets/Unmute.wav");
    }

    public void PlayNotificationSound() //todo: [FEAT++] Needs its own file
    {
        PlayMuteSound();
    }

    public bool IsRefreshNeeded()
    {
        if (_deviceReloadNeeded) return false;
        var devName = _playback.GetPlaybackName();
        return devName is null || devName != _config.Debug_InfoNoiseSpeakerName;
    }
    #endregion
}