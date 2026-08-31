using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Osc.Query;
using HoscyCore.Services.Osc.Relay;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class OscSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial bool ListeningPortUnapplied { get; protected set; }
    public virtual void ListeningPortChanged() { }

    [ObservableProperty]
    public partial string RelayFiltersInvalid { get; protected set; }
    public virtual void RelayFiltersClicked() { }
    public virtual void ReloadListenerClicked() { }

    public virtual void QueryServicesClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(OscSubMenuViewModelBase), Lifetime.Transient)]
public class OscSubMenuViewModelImpl : OscSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly PopupWindowFactory _popup;
    private readonly OscQueryHostRegistry _queryHosts;
    private readonly IOscListenService _oscListen;
    private readonly IOscQueryService _oscQuery;
    private readonly IOscRelayService _oscRelay;

    public OscSubMenuViewModelImpl
    (
        ConfigModel config,
        ILogger logger,
        PopupWindowFactory popup,
        IBackToFrontNotifyService notify,
        OscQueryHostRegistry queryHosts,
        IOscListenService oscListen,
        IOscQueryService oscQuery,
        IOscRelayService oscRelay
    )
    {
        Config = config;
        _logger = logger.ForContext<OscSubMenuViewModelImpl>();
        _popup = popup;
        _queryHosts = queryHosts;
        _oscListen = oscListen;
        _oscQuery = oscQuery;
        _oscRelay = oscRelay;

        CheckListeningPortUnapplied();
        CheckRelayFilterValidity(false)
            .IfFail(x => notify.SendResult("Failed to check relay filter validity", x));
    }

    private void CheckListeningPortUnapplied()
    {
        var port = _oscListen.GetPort();
        if (!port.IsOk)
        {
            _logger.Warning("Failed to retrieve OSC listener port, marking as unapplied ({res})", port.Msg);
            ListeningPortUnapplied = true;
            return;
        }
        ListeningPortUnapplied = port.Value != Config.Osc_Routing_ListenPort;
    }
    public override void ListeningPortChanged()
    {
        CheckListeningPortUnapplied();
    }
    public override void ReloadListenerClicked()
    {
        _logger.Information("Manually reloading OSC Listener and Query");

        var res = _oscQuery.Stop();
        if (!res.IsOk) 
        {
            _popup.OpenNotification("Failed to stop OSC Query", res.Msg.Message, true, true, null);
            return;
        }

        res = _oscListen.Stop();
        res = res.IsOk ? _oscListen.Start() : res;
        if (!res.IsOk)
        {
            _popup.OpenNotification("Failed to reloading OSC Listener", res.Msg.Message, true, true, null);
            return;
        }
        
        res = _oscQuery.Start();
        if (!res.IsOk)
        {
            _popup.OpenNotification("Failed to start OSC Query", res.Msg.Message, true, true, null);
            return;
        }

        CheckListeningPortUnapplied();
    }

    public override void RelayFiltersClicked()
    {
        _logger.Information("Editing osc relay filters");
        _popup.OpenEditOscRelayFilters(Config.Osc_Relay_Filters, null, RelayFiltersClosed);
    }
    private void RelayFiltersClosed()
    {
        var strings = CheckRelayFilterValidity(true);
        if (!strings.IsOk)
        {
            _popup.OpenNotification("Failed to check filter validity", strings.Msg.Message, true, true);
            return;
        }

        if (strings.Value.Length > 0)
        {
            var msg = $"Following relay filters are invalid:\n{string.Join("\n", strings.Value.Select(x => $" - {x}"))}";
            _popup.OpenNotification("Invalid relay filters found", msg, false, true);
        }
        Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
    }
    private Res<string[]> CheckRelayFilterValidity(bool reload)
    {
        var res = reload ? _oscRelay.ReloadFilters() : null;
        var invalidNames = _oscRelay.GetInvalidFilterNames();
        RelayFiltersInvalid = invalidNames.Length > 0 ? $"({invalidNames.Length} Relay{(invalidNames.Length == 1 ? "" : "s")} Invalid)" : string.Empty;
        return res?.IsOk ?? true ? ResC.TOk(invalidNames) : ResC.TFail<string[]>(res.Msg);
    }

    public override void QueryServicesClicked()
    {
        var endpoints = _queryHosts.GetServiceNames();
        _popup.OpenDisplayList("Viewing OSC Query Services", "Service Name", endpoints, null);
    }
}

#if DEBUG
public class OscSubMenuViewModelPreview : OscSubMenuViewModelBase
{
    public OscSubMenuViewModelPreview()
    {
        Config = new();
        RelayFiltersInvalid = "(1 Relay Invalid)";
    }
}
#endif