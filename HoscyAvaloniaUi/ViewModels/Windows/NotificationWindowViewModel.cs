using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public abstract void OnClipboardClick(IClipboard? clipboard, string text);
    public abstract void OnGithubClick();
}

[LoadIntoDiContainer(typeof(NotificationWindowViewModelBase), Lifetime.Transient)]
public class NotificationWindowViewModelImpl(ILogger logger) : NotificationWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<NotificationWindowViewModelImpl>();
    public override void OnClipboardClick(IClipboard? clipboard, string text)
    {
        _logger.Debug("Received clipboard copy request");
        
        if (clipboard is null)
        {
            _logger.Warning("Clipboard copy request failed, no clipboard available");
            return;
        }

        var res = ResC.WrapR(clipboard.SetTextAsync(text).AsSync, "Clipboard copy failed", _logger);
        if (res.IsOk)
        {
            _logger.Debug("Clipboard copy request succeeded");
        }
    }

    public override void OnGithubClick()
    {
        OtherUtils.OpenGithub(_logger);
    }
}

#if DEBUG
public class NotificationWindowViewModelPreview : NotificationWindowViewModelBase
{
    public override void OnClipboardClick(IClipboard? clipboard, string text) { }
    public override void OnGithubClick() { }
}
#endif