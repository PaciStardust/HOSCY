using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyAvaloniaUi.ViewModels.Core;

public partial class SplashScreenViewModel : ViewModelBase //todo: [FEAT] Open log on error?
{

    [ObservableProperty]
    public partial string Progress { get; set; } = "Unknown progress";

    [ObservableProperty]
    public partial string VersionText { get; set; } = "v.?.?.?";
}