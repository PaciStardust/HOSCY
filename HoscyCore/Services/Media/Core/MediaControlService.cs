using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Media.Core;

[PrototypeLoadIntoDiContainer(typeof(IMediaControlService))] //todo: [TEST]
public class MediaControlService
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<IMediaBackendStartInfo> infoLoader,
    IContainerBulkLoader<IMediaBackend> moduleLoader,
    ConfigModel config
) 
    : SoloModuleManagerBase<IMediaBackendStartInfo, IMediaBackend>
        (notify, logger.ForContext<MediaControlService>(), infoLoader, moduleLoader), IMediaControlService
{
    #region Injected
    private readonly ConfigModel _config = config;
    #endregion

    #region Start / Stop
    protected override Res OnModulePostStart(IMediaBackend module)
    {
        module.OnMediaUpdate += OnModuleMediaUpdate;
        return ResC.Ok();
    }
    protected override bool ShouldStartModelOnStartup()
        => true;

    protected override string GetSelectedModuleName()
    {
        var preferredBackend = _config.Media_Backend;

        var preferredMatch = GetModuleInfos().FirstOrDefault(x => x.Name.Equals(preferredBackend, StringComparison.OrdinalIgnoreCase));
        if (preferredMatch is not null) return preferredMatch.Name;

        preferredMatch = GetModuleInfos().FirstOrDefault(x => x.ConfigFlags != MediaBackendConfigFlags.None);
        if (preferredMatch is not null) return preferredMatch.Name;

        preferredMatch = GetModuleInfos().FirstOrDefault(x => x.ConfigFlags == MediaBackendConfigFlags.None);
        return preferredMatch?.Name ?? string.Empty;
    }

    protected override void UnsubscribeFromModuleEventsInternal(IMediaBackend module)
    {
        module.OnMediaUpdate -= OnModuleMediaUpdate;
    }
    #endregion

    #region Events
    public event Action<MediaUpdateInfo> OnMediaUpdate = delegate { };
    private void OnModuleMediaUpdate(MediaUpdateInfo info)
    {
        OnMediaUpdate.Invoke(info);
    }
    #endregion

    #region Control
    public bool CanGetEndpoints 
        => _currentModule?.CanGetEndpoints ?? false;
    public async Task<Res<string[]>> GetEndpointNamesAsync()
    {
        if (_currentModule is null)
            return ResC.TFailLog<string[]>("No Media backend is available to retrieve endpoints from", _logger, lvl: ResMsgLvl.Warning);
        return await _currentModule.GetEndpointNamesAsync();
    }

    public async Task<Res> PlayAsync()
    {
        if (_currentModule is null)
            return CommandErrorNoBackend("Play");

        var res = await _currentModule.PlayAsync();
        if (!res.IsOk) MediaCommandFailNotify(res.Msg, "Pause");
        else ClearFault();
        return res;
    }

    public async Task<Res> PauseAsync()
    {
        if (_currentModule is null)
            return CommandErrorNoBackend("Pause");

        var res = await _currentModule.PauseAsync();
        if (!res.IsOk) MediaCommandFailNotify(res.Msg, "Pause");
        else ClearFault();
        return res;
    }

    public async Task<Res> NextAsync()
    {
        if (_currentModule is null)
            return CommandErrorNoBackend("Next");

        var res = await _currentModule.NextAsync();
        if (!res.IsOk) MediaCommandFailNotify(res.Msg, "Next");
        else ClearFault();
        return res;
    }

    public async Task<Res> PreviousAsync()
    {
        if (_currentModule is null)
            return CommandErrorNoBackend("Previous");

        var res = await _currentModule.PreviousAsync();
        if (!res.IsOk) MediaCommandFailNotify(res.Msg, "Previous");
        else ClearFault();
        return res;
    }

    public async Task<Res> PlayPauseAsync()
    {
        if (_currentModule is null)
            return CommandErrorNoBackend("Toggle");

        var res = await _currentModule.PlayPauseAsync();
        if (!res.IsOk) MediaCommandFailNotify(res.Msg, "Toggle");
        else ClearFault();
        return res;
    }

    private Res CommandErrorNoBackend(string action)
    {
        var err = ResC.FailLog($"Unable to execute media command \"{action}\", no backend available", _logger, lvl: ResMsgLvl.Warning);
        MediaCommandFailNotify(err.Msg!, action);
        return err;
    }

    private void MediaCommandFailNotify(ResMsg msg, string action)
    {
        SetFaultLogNotify(msg, $"Media command \"{action}\" failed", _notify, null);
    }

    #endregion
}