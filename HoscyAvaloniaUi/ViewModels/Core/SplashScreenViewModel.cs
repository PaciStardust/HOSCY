using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Utility;

namespace HoscyAvaloniaUi.ViewModels.Core;

public partial class SplashScreenViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Progress { get; set; } = "Unknown progress";

    [ObservableProperty]
    public partial bool ErrorCopyVisible { get; set; } = false;

    [ObservableProperty]
    public partial string VersionText { get; set; } = "v.?.?.?";

    public void CopyErrorClicked(IClipboard? clipboard)
    {
        ClipboardUtil.CopyToClipboard(null, clipboard, Progress);
    }
}