#if WINDOWS

using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;
using Serilog;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Control;

namespace HoscyCore.Services.Media.Backends;

[PrototypeLoadIntoDiContainer(typeof(WindowsMediaBackendStartInfo))]
public class WindowsMediaBackendStartInfo : IMediaBackendStartInfo
{
    public MediaBackendConfigFlags ConfigFlags => MediaBackendConfigFlags.Windows;
    public string Name => "Windows";
    public string Description => "Standard Windows backend";
    public Type ModuleType => typeof(WindowsMediaBackend);
}

[PrototypeLoadIntoDiContainer(typeof(WindowsMediaBackend), Lifetime.Transient)]
public class WindowsMediaBackend(ILogger logger) : MediaBackendBase(logger.ForContext<WindowsMediaBackend>())
{
    #region Vars
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _manager is not null || _session is not null;
    protected override bool IsProcessing()
        => _manager is not null && _session is not null;

    protected override Res StartForService()
    {
        var managerRes = ResC.TWrapR(() => GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().AsSync(),
            "Failed to retrieve media session manager", _logger);
        if (!managerRes.IsOk) return ResC.Fail(managerRes.Msg);
        _manager = managerRes.Value;

        _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
        UpdateCurrentSession(_manager);
        
        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule()
    {
        ClearSession();
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _session = null;
        _manager = null;
    }
    #endregion

    #region Session
    private void ClearSession()
    {
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            _session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
            _session = null;
        }
    }
    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSessionManager sender)
    {
        ClearSession();

        _session = sender.GetCurrentSession();
        _logger.Debug("Media session has been changed to {session}", _session?.SourceAppUserModelId ?? "None");

        if (_session == null)
            return;

        _session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
        _session.PlaybackInfoChanged += Session_PlaybackInfoChanged;

        UpdateCurrentlyPlayingMediaProxy(_session);
    }
    #endregion

    #region Events
    private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        => UpdateCurrentlyPlayingMediaProxy(sender);

    /// <summary>
    /// Triggers on song switch
    /// </summary>
    private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        => UpdateCurrentlyPlayingMediaProxy(sender);

    /// <summary>
    /// Triggers when a new session is detected
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        => UpdateCurrentSession(sender);

    private void UpdateCurrentlyPlayingMediaProxy(GlobalSystemMediaTransportControlsSession sender)
        => UpdateCurrentlyPlayingMedia(sender).RunWithoutAwait();
    #endregion

    #region Media Updates
    private async Task UpdateCurrentlyPlayingMedia(GlobalSystemMediaTransportControlsSession sender)
    {
        var updateInfo = new MediaUpdateInfo();
        var playbackInfo = sender.GetPlaybackInfo();
        var newPlaying = await sender.TryGetMediaPropertiesAsync();

        updateInfo.Playing = (newPlaying is null || newPlaying.PlaybackType == MediaPlaybackType.Video || newPlaying.PlaybackType == MediaPlaybackType.Music) 
            && playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; 

        if (newPlaying is not null)
        {
            MediaUpdateInfoTrack? trackInfo = null;
            if (!string.IsNullOrWhiteSpace(newPlaying.Artist))
            {
                trackInfo ??= new();
                trackInfo.Artists = [newPlaying.Artist];
            }
            if (!string.IsNullOrWhiteSpace(newPlaying.Title))
            {
                trackInfo ??= new();
                trackInfo.Title = newPlaying.Title;
            }
            if (!string.IsNullOrWhiteSpace(newPlaying.AlbumTitle))
            {
                trackInfo ??= new();
                trackInfo.Album = newPlaying.AlbumTitle;
            }
            updateInfo.Track = trackInfo;
        }

        InvokeMediaUpdate(updateInfo);
    }
    #endregion

    #region Control
    public override bool CanGetEndpoints => false;
    public override Task<Res<string[]>> GetEndpointNames() 
        => Task.FromResult(ResC.TOk<string[]>([]));

    private async Task<Res> PerformCommandInternalAsync(Func<GlobalSystemMediaTransportControlsSession, IAsyncOperation<bool>> action, string logAction)
    {
        _logger.Debug($"Executing Media command \"{logAction}\"");

        if (_manager is null)
            return ResC.FailLog($"Unable to execute Media command \"{logAction}\", no manager is available",
                _logger, lvl: ResMsgLvl.Warning);

        UpdateCurrentSession(_manager);

        if (_session is null)
        {
            _logger.Debug("Media command \"{action}\" skipped, no session found", logAction);
            return ResC.Ok();
        }

        var res = await ResC.TWrapRAsync(action(_session).AsTask(), $"Failed to execute Media command \"{logAction}\"",
            _logger, ResMsgLvl.Warning);

        if (res.IsOk)
        {
            _logger.Debug($"Executed Media command \"{logAction}\"");
        }

        return ResC.Ok();
    }

    public override async Task<Res> NextAsync()
        => await PerformCommandInternalAsync(x => x.TrySkipNextAsync(), "SkipNext");
    public override async Task<Res> PreviousAsync()
        => await PerformCommandInternalAsync(x => x.TrySkipPreviousAsync(), "SkipPrevious");
    public override async Task<Res> PauseAsync()
        => await PerformCommandInternalAsync(x => x.TryPauseAsync(), "Pause");
    public override async Task<Res> PlayAsync()
        => await PerformCommandInternalAsync(x => x.TryPlayAsync(), "Play");
    public override async Task<Res> PlayPauseAsync()
        => await PerformCommandInternalAsync(x => x.TryTogglePlayPauseAsync(), "Toggle");
    #endregion
}

#endif