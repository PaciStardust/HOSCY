using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(IApplicationSound))]
public class AvaloniaApplicationSound : StartStopServiceBase, IApplicationSound, IAutoStartStopService
{
    #region Injects
    private readonly ConfigModel _config;
    private readonly IAudioService _audio;
    private readonly IBackToFrontNotifyService _notify;
    private readonly SwappableAudioPlaybackDevice<byte[]> _playback;

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
    private Task? _processingTask = null;
    private volatile bool _shouldTaskRun = true;
    private byte[] _nextToProcess = [];
    private readonly Lock _nextToProcessLock = new();
    private bool _deviceReloadNeeded = true;
    #endregion

    #region Startup
    protected override bool IsStarted()
        => _processingTask is not null;
    protected override bool IsProcessing()
        => IsStarted() && _playback is not null && _playback.IsPlaybackRunning;
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StartForService()
    {
        Interlocked.Exchange(ref _shouldTaskRun, true);
        _processingTask = Task.Run(RunProcessingLoop);
        return ResC.Ok();
    }

    protected override Res StopForService()
    {
        List<ResMsg> messages = [];

        Interlocked.Exchange(ref _shouldTaskRun, false);
        _playback.CancelCurrentAudio().IfFail(messages.Add);

        LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_processingTask, 500,
            new StartStopServiceException("Unable to stop processing loop"), _logger)
            .IfFail(messages.Add);

        _playback.CancelCurrentAudio().IfFail(messages.Add);

        return messages.Count == 0 ? ResC.Ok() : ResC.FailM(messages);
    }

    protected override void DisposeCleanup() //todo: Fix volatile, threads, tasks, thread safety in other areas
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
                        _nextToProcess = [];
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

            var toProcess = _nextToProcess.ToArray();
            _nextToProcess = [];
            _nextToProcessLock.Exit();

            var playbackRes = await _playback.PlayAsync(_config.Debug_InfoNoiseVolumePercent, toProcess, WriteNextToProcess);
            if (!playbackRes.IsOk)
            {
                SetFaultLogNotify(playbackRes.Msg, "Failed to play audio", _notify, _logger);
                await Task.Delay(10_000);
            }
        }
    }
    private static Task<Res> WriteNextToProcess(MemoryStream stream, CancellationToken _, byte[] toProcess)
    {
        stream.Write(toProcess);
        return Task.FromResult(ResC.Ok());
    }
    #endregion

    #region Writing
    private void SetNextAudio(byte[] newAudio)
    {
        var res = _playback.CancelCurrentAudio();
        if (!res.IsOk)
        {
            SetFaultLogNotify(res.Msg, "Failed to cancel last audio", _notify, _logger);
            return;
        }
        lock(_nextToProcessLock)
        {
            _nextToProcess = newAudio;
        }
    }

    public void PlayMuteSound()
    {
        return;
    }

    public void PlayNotificationSound()
    {
        return;
    }
    #endregion
}