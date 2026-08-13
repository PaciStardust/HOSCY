using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Afk;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class ExtrasSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    public virtual void AfkSkipClicked() { }
    public virtual void EditCountersClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(ExtrasSubMenuViewModelBase), Lifetime.Transient)]
public class ExtrasSubMenuViewModelImpl : ExtrasSubMenuViewModelBase
{
    private readonly IAfkService _afk;
    private readonly PopupWindowFactory _popup;

    public ExtrasSubMenuViewModelImpl(ConfigModel config, IAfkService afk, PopupWindowFactory popup)
    {
        Config = config;
        _afk = afk;
        _popup = popup;
    }

    public override void AfkSkipClicked()
    {
        _afk.StopAfk();
    }

    public override void EditCountersClicked()
    {
        _popup.OpenEditCounters(Config.Counters_List, null);
    }
}

#if DEBUG
public class ExtrasSubMenuViewModelPreview : ExtrasSubMenuViewModelBase
{
    public ExtrasSubMenuViewModelPreview()
    {
        Config = new();
    }
}
#endif