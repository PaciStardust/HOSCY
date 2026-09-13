using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class NotificationWindowViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial string? WindowTitle { get; set; } = "Notification Title";

    [ObservableProperty]
    public partial string Notification { get; set; } = "Notification Text";

    [ObservableProperty]
    public partial bool CopyClipboardVisible { get; set; } = true;

    public abstract void OnClipboardClick(IClipboard? clipboard);
    public abstract void OnGithubClick();
}

[LoadIntoDiContainer(typeof(NotificationWindowViewModelBase), Lifetime.Transient)]
public class NotificationWindowViewModelImpl(ILogger logger) : NotificationWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<NotificationWindowViewModelImpl>();
    public override void OnClipboardClick(IClipboard? clipboard)
    {
        ClipboardUtil.CopyToClipboard(_logger, clipboard, Notification);
    }

    public override void OnGithubClick()
    {
        OtherUtils.OpenGithub(_logger);
    }
}

#if DEBUG
public class NotificationWindowViewModelPreview : NotificationWindowViewModelBase
{
    public override void OnClipboardClick(IClipboard? clipboard) { }
    public override void OnGithubClick() { }
}
#endif