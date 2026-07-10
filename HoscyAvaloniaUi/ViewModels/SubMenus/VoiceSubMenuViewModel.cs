using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class VoiceSubMenuViewModelBase : ViewModelBase
{
    
}

[PrototypeLoadIntoDiContainer(typeof(VoiceSubMenuViewModelBase), Lifetime.Transient)]
public class VoiceSubMenuViewModelImpl : VoiceSubMenuViewModelBase
{
    
}

#if DEBUG
public class VoiceSubMenuViewModelPreview : VoiceSubMenuViewModelBase
{
    
}
#endif