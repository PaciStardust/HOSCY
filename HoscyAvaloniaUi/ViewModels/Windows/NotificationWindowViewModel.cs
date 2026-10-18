using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
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
    public partial string ButtonText { get; set; } = "Open Repository";
    public string? ButtonUrl { get; set; } = null;

    [ObservableProperty]
    public partial bool CopyClipboardVisible { get; set; } = true;

    public abstract void OnClipboardClick(IClipboard? clipboard);
    public abstract void OnRepoClick();
}

[LoadIntoDiContainer(typeof(NotificationWindowViewModelBase), Lifetime.Transient)]
public class NotificationWindowViewModelImpl(ILogger logger, IRepoService repoService) : NotificationWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<NotificationWindowViewModelImpl>();
    private readonly IRepoService _repoService = repoService;

    public override void OnClipboardClick(IClipboard? clipboard)
    {
        AvaloniaUtil.CopyToClipboard(_logger, clipboard, Notification);
    }

    public override void OnRepoClick()
    {
        OtherUtils.OpenWeb(ButtonUrl ?? _repoService.RepoUrl, _logger);
    }
}

#if DEBUG
public class NotificationWindowViewModelPreview : NotificationWindowViewModelBase
{
    public override void OnClipboardClick(IClipboard? clipboard) { }
    public override void OnRepoClick() { }
}
#endif