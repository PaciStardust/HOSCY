using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Afk;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class ExtrasSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    public virtual void AfkSkipClicked() { }
    public virtual void CountersEditClicked() { }

    [ObservableProperty]
    public partial string[] MediaBackendList { get; protected set; } = [];
    [ObservableProperty]
    public partial int MediaBackendIndex { get; set; }
    [ObservableProperty]
    public partial string MediaBackendDescription { get; protected set; } = string.Empty;
    [ObservableProperty]
    public partial bool MediaBackendHasEndpoints { get; protected set; }
    [ObservableProperty]
    public partial bool MediaBackendReloadNeeded { get; protected set; }
    [ObservableProperty]
    public partial bool MediaBackendIsLinuxMpris { get; protected set; }
    public virtual void MediaBackendChanged() { }
    public virtual void MediaBackendReloadClicked() { }
    public virtual void MediaBackendEndpointsClicked() { }
    public virtual void MediaFiltersClicked() { }
    public virtual void MediaMprisEndpointsPreferredClicked() { }
    public virtual void MediaMprisEndpointsIgnoredClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(ExtrasSubMenuViewModelBase), Lifetime.Transient)]
public class ExtrasSubMenuViewModelImpl : ExtrasSubMenuViewModelBase
{
    private readonly IAfkService _afk;
    private readonly PopupWindowFactory _popup;
    private readonly ILogger _logger;
    private readonly IMediaControlService _media;

    private readonly IMediaBackendStartInfo[] _mediaBackendInfos = [];

    public ExtrasSubMenuViewModelImpl
    (
        ConfigModel config,
        IAfkService afk, 
        PopupWindowFactory popup, 
        ILogger logger,
        IMediaControlService media
    )
    {
        Config = config;
        _afk = afk;
        _popup = popup;
        _logger = logger.ForContext<ExtrasSubMenuViewModelImpl>();
        _media = media;

        _mediaBackendInfos = [.. _media.GetModuleInfos().OrderByDescending(x => x.Priority)];
        var infoNames = _mediaBackendInfos.Select(x => x.Name);
        (MediaBackendList, MediaBackendIndex) = AvaloniaUiUtils.ComboBoxLoad([.. infoNames], Config.Media_Backend, _logger, "MediaBackend");
        MediaBackendUpdateMenus();
    }

    public override void AfkSkipClicked()
    {
        _afk.StopAfk();
    }

    public override void CountersEditClicked()
    {
        _popup.OpenEditCounters(Config.Counters_List, null);
        Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
    }

    public override void MediaBackendChanged()
    {
        MediaBackendUpdateMenus();
    }
    public override void MediaBackendEndpointsClicked()
    {
        if (!_media.CanGetEndpoints)
        {
            _popup.OpenNotification("Failed opening endpoints", "Unable to open endpoint viewer. This option should only be available if the module supports it but it does not", true, true);
            return;
        }

        var endpoints = _media.GetEndpointNames();
        if (!endpoints.IsOk)
        {
            _popup.OpenNotification("Failed retrieving endpoints", endpoints.Msg.Message, true, true);
            return;
        }

        _popup.OpenDisplayList("Module Endpoints", "Endpoint Name", endpoints.Value, null);
    }
    public override void MediaFiltersClicked()
    {
        _logger.Information("Editing media filters");
        _popup.OpenEditFilters(Config.Media_Filters, null);
        Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
    }
    public override void MediaBackendReloadClicked()
    {
        _logger.Information("Performing a media backend reload");
        
        var result = _media.RefreshModule();
        MediaBackendUpdateMenus();

        if (!result.IsOk)
        {
            _popup.OpenNotification("Media backend reload failed", result.Msg.Message, true, true);
        }
    }
    public override void MediaMprisEndpointsPreferredClicked()
    {
        _popup.OpenEditList(Config.Media_Mpris_PreferredEndpoints, "Editing preferred MPRIS endpoints", "Preferred MPRIS Endpoint", null);
    }
    public override void MediaMprisEndpointsIgnoredClicked()
    {
        _popup.OpenEditList(Config.Media_Mpris_IgnoredEndpoints, "Editing ignored MPRIS endpoints", "Ignored MPRIS Endpoint", null);
    }
    private void MediaBackendUpdateMenus()
    {
        MediaBackendIndex = MediaBackendIndex.MinMax(-1, MediaBackendList.Length - 1);
        var description = "Description: ";
        var selected = string.Empty;
        MediaBackendIsLinuxMpris = false;

        if (MediaBackendIndex == -1)
        {
            description += "No media backend is selected";
            Config.Media_Backend = string.Empty;
        } 
        else
        {
            var selectedName = MediaBackendList[MediaBackendIndex];
            var info = _mediaBackendInfos.FirstOrDefault(x => x.Name == selectedName);
            description += info?.Description ?? "Selected backend not found";
            Config.Media_Backend = info?.Name ?? string.Empty;
            MediaBackendIsLinuxMpris = info?.ConfigFlags.HasFlag(MediaBackendConfigFlags.LinuxMpris) ?? false;
        }
        MediaBackendDescription = description;

        var res = _media.GetCurrentModuleInfo();
        if (res is null)
        {
            MediaBackendReloadNeeded = !string.IsNullOrWhiteSpace(Config.Media_Backend);
            MediaBackendHasEndpoints = false;
        } 
        else
        {
            if (!res.IsOk)
            {
                MediaBackendReloadNeeded = true;
                MediaBackendHasEndpoints = false;
                _popup.OpenNotification("Failed to retrieve current module", res.Msg.Message, true, true);
            }
            else
            {
                MediaBackendHasEndpoints = _media.CanGetEndpoints;
                MediaBackendReloadNeeded = Config.Media_Backend != res.Value.Name;
            }
        }
    }
}

#if DEBUG
public class ExtrasSubMenuViewModelPreview : ExtrasSubMenuViewModelBase
{
    public ExtrasSubMenuViewModelPreview()
    {
        Config = new();
        MediaBackendList = [ "Test Backend" ];
        MediaBackendDescription = "Description Placeholder";
        MediaBackendReloadNeeded = true;
    }
}
#endif