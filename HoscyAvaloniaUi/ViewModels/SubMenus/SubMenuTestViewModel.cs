using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

[PrototypeLoadIntoDiContainer(typeof(SubMenuTestViewModel), Lifetime.Transient)]
public class SubMenuTestViewModel : ViewModelBase
{

}
