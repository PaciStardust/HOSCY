using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class InputSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(InputSubMenuViewModelBase), Lifetime.Transient)]
public class InputSubMenuViewModelImpl : InputSubMenuViewModelBase
{
    
}

#if DEBUG
public class InputSubMenuViewModelPreview : InputSubMenuViewModelBase
{
    
}
#endif