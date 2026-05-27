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
    private ConcurrentQueue<string>? _toProcess = null;
    private AudioPlaybackDeviceProxy? _playback = null;
    private readonly Lock _playbackAccessLock = new();
    private Task? _processingTask = null;
    #endregion

    #region Startup
    protected override bool IsStarted()
        => base.IsStarted() || _toProcess is not null || _playback is not null || _processingTask is not null;
    protected override bool IsProcessing()
        => base.IsProcessing() && _toProcess is not null && _playback is not null && _playback.IsRunning || _processingTask is not null;

    protected override Res StartForService()
    {
        var playback = _audio.CreatePlaybackDeviceProxy(_config.Voice_CurrentSpeakerName);
        if (!playback.IsOk) return ResC.Fail(playback.Msg);
        _playback = playback.Value;
        _playback.SetVolume(_config.Voice_AudioVolumePercent);

        _toProcess = [];
        _processingTask = Task.Run(RunProcessingLoop);

        return base.StartForService();
    }

    protected override Res StopForService()
    {
        List<ResMsg> messages = [];
        lock (_playbackAccessLock)
        {
            Clear();
            _toProcess = null;

            LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_processingTask, 500,
                new StartStopServiceException("Unable to stop processing loop"), _logger)
                .IfFail(messages.Add);
            
            _playback?.Stop(_logger).IfFail(messages.Add);
        }

        base.StopForService().IfFail(messages.Add);

        return messages.Count == 0 ? ResC.Ok() : ResC.FailM(messages);
    }

    protected override void DisposeCleanup()
    {
        _processingTask?.Dispose();
        _processingTask = null;

        if (_playback is not null)
        {
            _playback?.Dispose();
            _playback = null;
        }

        _toProcess = null;

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

    public Res SetVolume(float value) //todo: use config setting
    {
        lock (_playbackAccessLock)
        {
            if (_playback is null)
            return ResC.FailLog("Can not set volume, no playback is available", _logger);
            _config.Voice_AudioVolumePercent = value;
            _playback.SetVolume(_config.Voice_AudioVolumePercent);
        }
        return ResC.Ok();
    }

    public Res ChangePlayback(string name)
    {
        var newPlayback = _audio.CreatePlaybackDeviceProxy(name);
        if (!newPlayback.IsOk) return ResC.Fail(newPlayback.Msg);

        Res stopRes;
        lock (_playbackAccessLock)
        {
            stopRes = _playback?.Stop(_logger) ?? ResC.Ok();
            _playback?.Dispose();
            _playback = newPlayback.Value;
            _playback.SetVolume(_config.Voice_AudioVolumePercent);
        }
        return stopRes;
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

    private async Task RunProcessingLoop()
    {
        while (_toProcess is not null)
        {
            if (_toProcess.IsEmpty)
            {
                await Task.Delay(25);
                continue;
            }

            await Task.Delay(100);
            //todo: this
        }
    }
    #endregion
}