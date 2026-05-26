#if LINUX

using System.Diagnostics.CodeAnalysis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;
using Serilog;
using Tmds.DBus;

namespace HoscyCore.Services.Media.Backends;

[PrototypeLoadIntoDiContainer(typeof(LinuxMprisMediaBackendStartInfo), Lifetime.Singleton)]
public class LinuxMprisMediaBackendStartInfo : IMediaBackendStartInfo
{
    public MediaBackendConfigFlags ConfigFlags => MediaBackendConfigFlags.LinuxMpris;
    public string Name => "Linux Mpris";
    public string Description => "Linux Backend using the MPRIS D-Bus specification";
    public Type ModuleType => typeof(LinuxMprisMediaBackend);
}

[PrototypeLoadIntoDiContainer(typeof(LinuxMprisMediaBackend), Lifetime.Transient)]
public class LinuxMprisMediaBackend(ILogger logger, ConfigModel config) : MediaBackendBase(logger.ForContext<LinuxMprisMediaBackend>())
{
    #region Injected
    private readonly ConfigModel _config = config;
    #endregion

    #region Vars
    private Connection? _connection = null;

    private EndpointCache? _currentEndpoint = null;

    private Task? _refreshTask = null;
    private bool _stopRefreshTask = false;
    #endregion

    #region Start / Stop
    [MemberNotNullWhen(true, nameof(_connection))]
    protected override bool IsStarted() => _connection is not null;

    [MemberNotNullWhen(true, nameof(_connection))]
    protected override bool IsProcessing() => IsStarted();

    protected override Res StartForService()
    {
        _connection = Connection.Session;

        var connectRes = ConnectDbus();
        if (!connectRes.IsOk) return connectRes;

        _connection.StateChanged += OnStateChanged;

        _stopRefreshTask = false;
        _refreshTask = Task.Run(RunRefreshLoop);

        return ResC.Ok();
    }

    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule()
    {
        _connection?.StateChanged -= OnStateChanged;

        _stopRefreshTask = true;
        return LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_refreshTask, 100,
            new StartStopServiceException("Unable to stop refresh loop"), _logger);
    }
    protected override void DisposeCleanup()
    {
        _refreshTask?.Dispose();
        _refreshTask = null;

        _currentEndpoint?.OnChanged.Dispose();
        _currentEndpoint = null;

        _connection?.Dispose();
        _connection = null;
    }
    #endregion

    #region Connect
    private Res ConnectDbus()
    {
        if (_connection is null)
            return ResC.Fail("Unable to establish D-Bus connection, the connection has been disposed");

        return ResC.WrapR(() => _connection.ConnectAsync().AsSync(),
            "Failed to connect to D-Bus", _logger);
    }
    #endregion

    #region Endpoint Update
    private const string MPRIS_ID = "org.mpris.MediaPlayer2.";
    public override async Task<Res<string[]>> GetEndpointNamesAsync()
    {
        if (!IsProcessing())
            return ResC.TFailLog<string[]>(message: "Failed to grab MPRIS endpoints, not connected", _logger);

        var services = await ResC.TWrapRAsync(_connection.ListServicesAsync(), "Failed to grab MPRIS endpoints", _logger);
        if (!services.IsOk) return services;

        var endpoints = services.Value.Where(x => x.StartsWith(MPRIS_ID, StringComparison.OrdinalIgnoreCase));

        return ResC.TOk(endpoints.ToArray());
    }
    public override bool CanGetEndpoints => true;

    private async Task<Res<string>> GetBestEndpointName()
    {
        var allEndpoints = await GetEndpointNamesAsync();
        if (!allEndpoints.IsOk) return ResC.TFail<string>(allEndpoints.Msg);

        var filteredEndpoints = allEndpoints.Value.Where(
            x => !_config.Media_Mpris_IgnoredEndpoints.Any(y => y.Contains(x, StringComparison.OrdinalIgnoreCase))
        ).ToArray();

        if (filteredEndpoints.Length == 0)
            return ResC.TOk(string.Empty);

        var bestOption = filteredEndpoints.FirstOrDefault(
            x => _config.Media_Mpris_PreferredEndpoints.Any(y => x.Contains(y, StringComparison.OrdinalIgnoreCase))
        );

        return ResC.TOk(string.IsNullOrWhiteSpace(bestOption) ? filteredEndpoints[0] : bestOption);
    }

    private async Task<Res> UpdateCurrentEndpoint()
    {
        var newEndpoint = await GetBestEndpointName();
        if (!newEndpoint.IsOk) return ResC.Fail(newEndpoint.Msg);

        var oldEndpointName = _currentEndpoint?.Name ?? string.Empty;
        if (oldEndpointName == newEndpoint.Value)
            return ResC.Ok();

        _currentEndpoint?.OnChanged.Dispose();
        _currentEndpoint = null;

        if (string.IsNullOrWhiteSpace(newEndpoint.Value))
        {
            _logger.Debug("No valid endpoints found, clearing");
            return ResC.Ok();
        }

        _logger.Debug("Identified new Endpoint ({nameOld}) => ({name}), initializing...",
            oldEndpointName, newEndpoint.Value);

        var playerRes = ResC.TWrapR(() => _connection!.CreateProxy<IMprisPlayer>(newEndpoint.Value, "/org/mpris/MediaPlayer2"),
            $"Failed to create Player Proxy for Endpoint \"{_currentEndpoint}\"", _logger);
        if (!playerRes.IsOk) return ResC.Fail(playerRes.Msg);

        var propRes = await ResC.TWrapRAsync(playerRes.Value.GetAllAsync(),
            $"Failed to get properties for Endpoint \"{_currentEndpoint}\"", _logger);
        if (!propRes.IsOk) return ResC.Fail(propRes.Msg);

        var onChangeRes = await ResC.TWrapRAsync(playerRes.Value.WatchPropertiesAsync(OnPropertyChange),
            $"Failed to subscribe to changes for Endpoint \"{_currentEndpoint}\"", _logger);
        if (!onChangeRes.IsOk) return ResC.Fail(onChangeRes.Msg);
        
        _currentEndpoint = new(newEndpoint.Value, playerRes.Value, propRes.Value, onChangeRes.Value);
        _logger.Debug("Initialized new Endpoint ({oldEndpoint}) => ({name})", oldEndpointName, _currentEndpoint.Name);

        InvokeMediaUpdate(new MediaUpdateInfo()
        {
            Playing = HandlePropertyChangePlaying(_currentEndpoint.Properties.PlaybackStatus),
            Track = HandlePropertyChangeMetadata(_currentEndpoint.Properties.Metadata)
        });

        return ResC.Ok();
    }

    private async Task<Res<EndpointCache>?> UpdateAndGetCurrentEndpoint()
    {
        var updateRes = await UpdateCurrentEndpoint();
        if (!updateRes.IsOk)
            return ResC.TFail<EndpointCache>(updateRes.Msg);
        return _currentEndpoint is null ? null : ResC.TOk(_currentEndpoint);
    }
    #endregion

    #region Control
    private async Task<Res> DoActionAsync(Func<MprisPlayerProperties, bool> check, Func<IMprisPlayer, Task> action, string logAction)
    {
        _logger.Debug("Performing media action {action}", logAction);
        var endpointRes = await UpdateAndGetCurrentEndpoint();
        if (endpointRes is null)
        {
            _logger.Debug("Media action {action} skipped, no endpoint found", logAction);
            return ResC.Ok();
        }
        if (!endpointRes.IsOk)
        {
            _logger.Warning("Media action {action} skipped due to error in loading endpoint: {msg}", logAction, endpointRes.Msg);
            return ResC.Fail(endpointRes.Msg.WithContext($"Media {logAction.FirstCharToUpper()}"));
        }

        var res = await ResC.WrapRAsync(DoActionInternalAsync(endpointRes.Value, check, action, logAction),
            $"Media action {logAction} failed", _logger, ResMsgLvl.Warning);

        if (res.IsOk)
        {
            _logger.Debug("Performed media action {action}", logAction);
        }

        return res;
    }
    private async Task DoActionInternalAsync(EndpointCache cache, Func<MprisPlayerProperties, bool> check, Func<IMprisPlayer, Task> action, string logAction)
    {
        if (check(cache.Properties))
        {
            await action(cache.Player);
        }
        else
        {
            _logger.Warning("Media action {action} skipped, endpoint does not permit it", logAction);
        }
    }

    public override async Task<Res> PlayAsync()
        => await DoActionAsync(x => x.CanPlay, x => x.PlayAsync(), "play");
    public override async Task<Res> PauseAsync()
        => await DoActionAsync(x => x.CanPause, x => x.PauseAsync(), "pause");
    public override async Task<Res> NextAsync()
        => await DoActionAsync(x => x.CanGoNext, x => x.NextAsync(), "next");
    public override async Task<Res> PreviousAsync()
        => await DoActionAsync(x => x.CanGoPrevious, x => x.PreviousAsync(), "play");
    public override async Task<Res> PlayPauseAsync()
        => await DoActionAsync(x => x.CanControl, x => x.PlayPauseAsync(), "toggle");
    #endregion

    #region Event Handling
    private void OnPropertyChange(PropertyChanges changes)
    {
        MediaUpdateInfo? info = null;
        foreach(var change in changes.Changed)
        {
            HandlePropertyChange(change.Key, change.Value, ref info);
        }

        if (info is not null)
        {
            InvokeMediaUpdate(info);
        }
    }

    private void HandlePropertyChange(string key, object value, ref MediaUpdateInfo? info)
    {
        switch(key)
        {
            case "PlaybackStatus":
                if (value is string playback)
                {
                    info ??= new();
                    info.Playing = HandlePropertyChangePlaying(playback);
                }
                break;

            case "Metadata":
                if (value is Dictionary<string, object> meta)
                {
                    var track = HandlePropertyChangeMetadata(meta);
                    if (track is not null)
                    {
                        info ??= new();
                        info.Track = track;
                    }
                }
                break;

            case "CanGoNext":
                if (value is bool canNext)
                    _currentEndpoint?.Properties.CanGoNext = canNext;
                break;
            case "CanGoPrevious":
                if (value is bool canPrev)
                    _currentEndpoint?.Properties.CanGoPrevious = canPrev;
                break;
            case "CanPlay":
                if (value is bool canPlay)
                    _currentEndpoint?.Properties.CanPlay = canPlay;
                break;
            case "CanPause":
                if (value is bool canPause)
                    _currentEndpoint?.Properties.CanPause = canPause;
                break;
            case "CanControl":
                if (value is bool canCtl)
                    _currentEndpoint?.Properties.CanControl = canCtl;
                break;

            default:
                return;
        }
    }

    private bool HandlePropertyChangePlaying(string playing)
        => playing.Equals("Playing", StringComparison.OrdinalIgnoreCase);

    private MediaUpdateInfoTrack? HandlePropertyChangeMetadata(IDictionary<string, object> metadata)
    {
        MediaUpdateInfoTrack? track = null;
        foreach(var (key, value) in metadata)
        {
            switch (key.ToLower())
            {
                case "xesam:album":
                    if (value is string album && !string.IsNullOrWhiteSpace(album))
                    {
                        track ??= new();
                        track.Album = album;
                    }
                    break;

                case "xesam:artist":
                    if (value is string[] artistArr && artistArr.Length != 0)
                    {
                        track ??= new();
                        track.Artists = artistArr;
                    }
                    else if (value is string artistStr && !string.IsNullOrWhiteSpace(artistStr))
                    {
                        track ??= new();
                        track.Artists = [artistStr];
                    }
                    break;

                case "xesam:title":
                    if (value is string title && !string.IsNullOrWhiteSpace(title))
                    {
                        track ??= new();
                        track.Title = title;
                    }
                    break;
            }
        }

        return track;
    }
    #endregion

    #region Refresh
    private async Task RunRefreshLoop()
    {
        _logger.Debug("Starting refresh loop");

        var lastRefresh = DateTimeOffset.MinValue;
        while(!_stopRefreshTask)
        {
            await Task.Delay(10);

            if (DateTimeOffset.UtcNow < lastRefresh.AddMilliseconds(_config.Media_Mpris_EndpointUpdateIntervalMs))
                continue;
            
            await UpdateCurrentEndpoint();
        }

        _logger.Debug("Stopping refresh loop");
    }

    private void OnStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        _logger.Debug("Connection state has changed to {state}", e.State);
        if (e.DisconnectReason is not null)
        {
            _logger.Warning(e.DisconnectReason, "Disconnected with reason");
        }
    }
    #endregion

    #region Utils
    private class EndpointCache(string name, IMprisPlayer player, MprisPlayerProperties properties, IDisposable onChanged)
    {
        public string Name { get; init; } = name;
        public IMprisPlayer Player { get; init; } = player;
        public MprisPlayerProperties Properties { get; set; } = properties;
        public IDisposable OnChanged = onChanged;
    }
    #endregion
}

#endif