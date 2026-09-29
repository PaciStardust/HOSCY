using System.Collections.Concurrent;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Core;

[PrototypeLoadIntoDiContainer(typeof(IVoiceManagerService))]
public class VoiceManagerService 
    : SoloModuleManagerBase<IVoiceModuleStartInfo, IVoiceModule>, IVoiceManagerService
{
    #region Injects
    private readonly IAudioService _audio;
    private readonly ConfigModel _config;

    public VoiceManagerService
    (
        IBackToFrontNotifyService notify,
        ILogger logger,
        IContainerBulkLoader<IVoiceModuleStartInfo> infoLoader,
        IContainerBulkLoader<IVoiceModule> moduleLoader,
        IAudioService audio,
        ConfigModel config
    )
        :base(notify, logger.ForContext<VoiceManagerService>(), infoLoader, moduleLoader)
    {
        _config = config;
        _audio = audio;
        
        _playback = new(_logger, _audio);
    }
    #endregion

    #region Vars
    private SwappableAudioPlaybackDevice<string> _playback;
    private Task? _processingTask = null;
    private volatile bool _shouldTaskRun = true;
    private ConcurrentQueue<string> _toProcess = [];
    private volatile bool _deviceReloadNeeded = true;
    #endregion

    #region Startup
    protected override bool IsStarted()
        => base.IsStarted() || _processingTask is not null;
    protected override bool IsProcessing()
        => base.IsProcessing() && _playback.IsPlaybackRunning;

    protected override Res StartForService()
    {
        _playback.Dispose();
        _playback = new(_logger, _audio);

        _deviceReloadNeeded = true;
        _shouldTaskRun = true;
        _toProcess.Clear();

        _processingTask = Task.Run(RunProcessingLoop);

        return base.StartForService();
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

        _deviceReloadNeeded = false;
        _toProcess.Clear();
            
        base.StopForService().IfFail(messages.Add);

        return messages.Count == 0 ? ResC.Ok() : ResC.FailM(messages);
    }

    protected override void DisposeCleanup()
    {
        _processingTask?.Dispose();
        _processingTask = null;

        _playback?.Dispose();

        base.DisposeCleanup();
    }

    public event Action<ServiceStatus> OnModuleStatusChanged = delegate {};
    private void InvokeModuleStatusChanged()
    {
        var status = GetCurrentModuleStatus();
        _logger.Verbose("Triggering event for module status update started={started}", status);
        OnModuleStatusChanged.Invoke(status);
    }
    protected override void OnModuleStatusUpdate()
    {
        InvokeModuleStatusChanged();
    }
    #endregion

    #region Control
    protected override string GetSelectedModuleName()
        => _config.Voice_SelectedModuleName;
    protected override bool ShouldStartModelOnStartup()
        => _config.Voice_AutoStart;
    
    public Res Clear()
    {
        _logger.Debug("Clearing voice queue");
        _toProcess?.Clear();
        return _playback.CancelCurrentAudio();
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

    public string? GetPlaybackName()
    {
        return _playback.GetPlaybackName();
    }

    public void RefreshPlayback()
    {
        _logger.Debug("Set flag to refresh audio device");
        _deviceReloadNeeded = true;
    }

    public bool IsPlaybackRefreshNeeded()
    {
        if (_deviceReloadNeeded) return false;
        var devName = _playback.GetPlaybackName();
        return devName is null || devName != _config.Voice_CurrentSpeakerName;
    }

    private async Task RunProcessingLoop()
    {
        while (_shouldTaskRun)
        {
            if (_deviceReloadNeeded)
            {
                _deviceReloadNeeded = false;
                var swapRes = _playback.SwapPlayback(_config.Voice_CurrentSpeakerName);
                if (!swapRes.IsOk)
                {
                    SetFaultLogNotify(swapRes.Msg, "Failed to load speaker for voice audio", _notify, _logger);
                    _toProcess.Clear();
                }
            }

            if (_toProcess.Count == 0 || !_toProcess.TryDequeue(out var voiceString))
            {
                await Task.Delay(25);
                continue;
            }

            var playbackRes = await _playback.PlayAsync(_config.Voice_AudioVolumePercent, voiceString, WriteAudio);
            if (!playbackRes.IsOk)
            {
                SetFaultLogNotify(playbackRes.Msg, "Failed to play audio", _notify, _logger);
                await Task.Delay(10_000);
            }
        }
    }

    private async Task<Res> WriteAudio(MemoryStream stream, CancellationToken ct, string voiceString)
    {
        if (_currentModule is null) return ResC.Ok();

        var voiceRes = await ResC.WrapAsync(_currentModule.CreateAudio(voiceString, stream, ct), 
                "Failed to create audio", _logger);

        return voiceRes;
    }
    #endregion
}