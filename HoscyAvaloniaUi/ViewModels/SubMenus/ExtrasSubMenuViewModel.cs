using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class ExtrasSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(ExtrasSubMenuViewModelBase), Lifetime.Transient)]
public class ExtrasSubMenuViewModelImpl : ExtrasSubMenuViewModelBase
{
    
}

#if DEBUG
public class ExtrasSubMenuViewModelPreview : ExtrasSubMenuViewModelBase
{
    
}
#endif