using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class DebugSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(DebugSubMenuViewModelBase), Lifetime.Transient)]
public class DebugSubMenuViewModelImpl : DebugSubMenuViewModelBase
{
    
}

#if DEBUG
public class DebugSubMenuViewModelPreview : DebugSubMenuViewModelBase
{
    
}
#endif