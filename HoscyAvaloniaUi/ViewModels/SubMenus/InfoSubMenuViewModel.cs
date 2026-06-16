using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class InfoSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial IBrush ListeningStatusBrush { get; set; } = new SolidColorBrush(Colors.HotPink);
    [ObservableProperty]
    public partial string ListeningStatusText { get; set; } = "Muted";

    [ObservableProperty]
    public partial IBrush ActiveStatusBrush { get; set; } = new SolidColorBrush(Colors.HotPink);
    [ObservableProperty]
    public partial string ActiveStatusText { get; set; } = "Stopped";

    [ObservableProperty]
    public partial string SentViaText { get; set; } = "No message sent since opening";
    [ObservableProperty]
    public partial string MessageText { get; set; } = "No message sent since opening";
    [ObservableProperty]
    public partial string NotificationText { get; set; } = "No notification sent since opening";
}

[PrototypeLoadIntoDiContainer(typeof(InfoSubMenuViewModelBase), Lifetime.Transient)]
public class InfoSubMenuViewModelImpl : InfoSubMenuViewModelBase
{
    
}

#if DEBUG
public class InfoSubMenuViewModelPreview : InfoSubMenuViewModelBase
{
    
}
#endif