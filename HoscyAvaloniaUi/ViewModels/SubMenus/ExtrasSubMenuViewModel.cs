using CommunityToolkit.Mvvm.ComponentModel;
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
}

[PrototypeLoadIntoDiContainer(typeof(ExtrasSubMenuViewModelBase), Lifetime.Transient)]
public class ExtrasSubMenuViewModelImpl : ExtrasSubMenuViewModelBase
{
    private readonly IAfkService _afk;

    public ExtrasSubMenuViewModelImpl(ConfigModel config, IAfkService afk)
    {
        Config = config;
        _afk = afk;
    }

    public override void AfkSkipClicked()
    {
        _afk.StopAfk();
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