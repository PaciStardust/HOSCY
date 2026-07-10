using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class RecogSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(RecogSubMenuViewModelBase), Lifetime.Transient)]
public class RecogSubMenuViewModelImpl : RecogSubMenuViewModelBase
{
    
}

#if DEBUG
public class RecogSubMenuViewModelPreview : RecogSubMenuViewModelBase
{
    
}
#endif