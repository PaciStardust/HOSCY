using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class OscSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(OscSubMenuViewModelBase), Lifetime.Transient)]
public class OscSubMenuViewModelImpl : OscSubMenuViewModelBase
{
    
}

#if DEBUG
public class OscSubMenuViewModelPreview : OscSubMenuViewModelBase
{
    
}
#endif