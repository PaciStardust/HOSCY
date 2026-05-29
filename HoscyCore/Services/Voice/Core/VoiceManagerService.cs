using System.Collections.Concurrent;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Core;

[PrototypeLoadIntoDiContainer(typeof(IVoiceManagerService))] //todo: [TEST] this
public class VoiceManagerService
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<IVoiceModuleStartInfo> infoLoader,
    IContainerBulkLoader<IVoiceModule> moduleLoader,
    IAudioService audio,
    ConfigModel config
) 
    : SoloModuleManagerBase<IVoiceModuleStartInfo, IVoiceModule>
        (notify, logger.ForContext<VoiceManagerService>(), infoLoader, moduleLoader),
    IVoiceManagerService
{
    #region Injects
    private readonly IAudioService _audio = audio;
    private readonly ConfigModel _config = config;
    #endregion

    #region Vars
    private Task? _processingTask = null;

    private ConcurrentQueue<string>? _toProcess = null;

    private AudioPlaybackDeviceProxy? _playback = null;
    private CancellationTokenSource? _cts = null;
    private volatile bool _isPlaying = false;

    #endregion

    #region Startup
    protected override bool IsStarted()
        => base.IsStarted() || _toProcess is not null || _playback is not null || _processingTask is not null;
    protected override bool IsProcessing()
        => base.IsProcessing() && _toProcess is not null && _playback is not null && _playback.IsRunning || _processingTask is not null;

    protected override Res StartForService()
    {
        var createRes = CreateCurrentPlayback();
        if (!createRes.IsOk) return createRes;

        _toProcess = [];
        _processingTask = Task.Run(RunProcessingLoop);

        return base.StartForService();
    }

    protected override Res StopForService()
    {
        List<ResMsg> messages = [];

        ClearCurrentPlayback().IfFail(messages.Add);

        Clear();
        _toProcess = null;

        LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_processingTask, 500,
            new StartStopServiceException("Unable to stop processing loop"), _logger)
            .IfFail(messages.Add);
            
        base.StopForService().IfFail(messages.Add);

        return messages.Count == 0 ? ResC.Ok() : ResC.FailM(messages);
    }

    protected override void DisposeCleanup()
    {
        _processingTask?.Dispose();
        _processingTask = null;

        _playback?.Dispose();
        _playback = null;

        _toProcess = null;

        _cts?.Dispose();
        _cts = null;

        base.DisposeCleanup();
    }
    #endregion

    #region Control
    protected override string GetSelectedModuleName()
        => _config.Voice_SelectedModuleName;
    protected override bool ShouldStartModelOnStartup()
        => _config.Voice_AutoStart;
    
    public void Clear()
    {
        _logger.Debug("Clearing voice queue");
        _toProcess?.Clear();
    }

    public Res ChangePlayback(string name)
    {
        List<ResMsg> messages = [];

        ClearCurrentPlayback().IfFail(messages.Add);
        CreateCurrentPlayback().IfFail(messages.Add);

        return messages.Count > 0 ? ResC.FailM(messages) : ResC.Ok();
    }
    #endregion

    #region Processing
    public Res Enqueue(string message)
    {
        if (_toProcess is null)
            return ResC.FailLog("Unable to enqueue text to voice, queue is not available", _logger, lvl: ResMsgLvl.Warning);

        if (message.Length > _config.Voice_MaximumTextLength)
        {
            if (_config.Voice_SkipLongerText)
            {
                _logger.Warning("Skipping message for voice, length {len} > max {lenMax}: {text}",
                    message.Length, _config.Voice_MaximumTextLength, message);
                return ResC.Ok();
            }

            var oldMessage = message;
            message = OtherUtils.TrimBySpace(message, _config.Voice_MaximumTextLength);
            _logger.Debug("Trimmed voice message \"{oldMsg}\" to \"{msg}\" as it was too long", oldMessage, message);
        }

        _toProcess.Enqueue(message);
        return ResC.Ok();
    }

    private Res CreateCurrentPlayback()
    {
        _logger.Debug("Creating current playback");

        if (_cts is not null || _playback is not null)
            return ResC.FailLog("Unable to create playback, it already exists", _logger);

        var playback = _audio.CreatePlaybackDeviceProxy(_config.Voice_CurrentSpeakerName, _logger);
        if (!playback.IsOk) return ResC.Fail(playback.Msg);
        _playback = playback.Value;

        var playbackOn = _playback.Start();
        if (!playbackOn.IsOk) return playbackOn;

        _cts = new();

        return ResC.Ok();
    }

    private Res ClearCurrentPlayback()
    {
        List<ResMsg> playbackErrors = [];
                
        _logger.Debug("Clearing current playback");
        if (_cts is not null)
        {
            _logger.Debug("Stopping current playing if needed");
            _cts.Cancel();
            if (!OtherUtils.WaitWhile(() => _isPlaying, 20_000, 10))
            {
                _isPlaying = false;
                var res = ResC.FailLog("Playback failed to stop after 30s", _logger, lvl: ResMsgLvl.Warning);
                playbackErrors.Add(res.Msg!);
                _cts = null;
            }
        }

        _playback?.Stop().IfFail(playbackErrors.Add);
        _playback?.Dispose();
        _playback = null;

        return playbackErrors.Count > 0 ? ResC.FailM(playbackErrors) : ResC.Ok();
    }

    private async Task RunProcessingLoop()
    {
        while (_toProcess is not null)
        {
            _isPlaying = false;
            if (_toProcess.IsEmpty || (_cts?.IsCancellationRequested ?? true) || !_toProcess.TryDequeue(out var voiceString))
            {
                await Task.Delay(25);
                continue;
            }

            if (_currentModule is null || _playback is null)
            {
                _logger.Warning("Component missing for processing, performing clear");
                Clear();
                continue;
            }

            _isPlaying = true;
            _playback.Stream.SetLength(0);
            var voiceRes = await ResC.WrapAsync(_currentModule.CreateAudio(voiceString, _playback.Stream, _cts.Token), 
                "Failed to create audio", _logger);
            if (!voiceRes.IsOk)
            {
                _playback.Stream.SetLength(0);
                _isPlaying = false;
                SetFaultLogNotify(voiceRes.Msg, "Failed to play audio", _notify, _logger);
                await Task.Delay(10000);
                continue;
            }

            var playbackRes = await _playback.PlayAsync(_cts.Token, _config.Voice_AudioVolumePercent);
            _playback.Stream.SetLength(0);
            _isPlaying = false;
                
            if (!playbackRes.IsOk)
            {
                SetFaultLogNotify(playbackRes.Msg, "Failed to play audio", _notify, _logger);
                await Task.Delay(10000);
            }
        }
    }
    #endregion
}