using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class TransSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(TransSubMenuViewModelBase), Lifetime.Transient)]
public class TransSubMenuViewModelImpl : TransSubMenuViewModelBase
{
    
}

#if DEBUG
public class TransSubMenuViewModelPreview : TransSubMenuViewModelBase
{
    
}
#endif