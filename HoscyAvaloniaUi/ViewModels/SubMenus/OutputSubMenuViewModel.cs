using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class OutputSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(OutputSubMenuViewModelBase), Lifetime.Transient)]
public class OutputSubMenuViewModelImpl : OutputSubMenuViewModelBase
{
    
}

#if DEBUG
public class OutputSubMenuViewModelPreview : OutputSubMenuViewModelBase
{
    
}
#endif