using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Components;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.SubMenus;
using HoscyAvaloniaUi.Views.SubMenus;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Core;

public abstract partial class CoreMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial List<NavigationButton> NavButtons { get; set; } = [];

    [ObservableProperty]
    public partial UserControl CurrentSubmenu { get; set; } = new();
    
    [ObservableProperty]
    public partial bool BannerVisible { get; set; } = false;

    [ObservableProperty]
    public partial string BannerMessage { get; set; } = "Banner Message\n1\n2\n3\n4";

    [ObservableProperty]
    public partial bool BannerColorAccent { get; set; } = false;

    public abstract void OnMenuSelected(ListBox listBox);
    public void HideBanner()
    {
        BannerVisible = false;
        BannerMessage = string.Empty;
    }
}


[LoadIntoDiContainer(typeof(CoreMenuViewModelBase), Lifetime.Transient)]
public partial class CoreMenuViewModelImpl : CoreMenuViewModelBase
{
    private record NavButtonInfo(Color Color, Func<UserControl> ControlGenerator, Type ControlType);
    private static readonly Dictionary<string, NavButtonInfo> _buttonInfos = new() {
        { "Info",   new(Color.FromUInt32(0x_FFFFADAD), () => new InfoSubMenu(),     typeof(InfoSubMenuViewModelBase)) },
        { "Input",  new(Color.FromUInt32(0x_FFFFD6A5), () => new InputSubMenu(),    typeof(InputSubMenuViewModelBase)) },
        { "Output", new(Color.FromUInt32(0x_FFFDFFB6), () => new OutputSubMenu(),   typeof(OutputSubMenuViewModelBase)) },
        { "Recog",  new(Color.FromUInt32(0x_FFCAFFBF), () => new RecogSubMenu(),    typeof(RecogSubMenuViewModelBase)) },
        { "Voice",  new(Color.FromUInt32(0x_FF9BF6FF), () => new VoiceSubMenu(),    typeof(VoiceSubMenuViewModelBase)) },
        { "Trans",  new(Color.FromUInt32(0x_FFA0C4FF), () => new TransSubMenu(),    typeof(TransSubMenuViewModelBase)) },
        { "OSC",    new(Color.FromUInt32(0x_FFDBB2FF), () => new OscSubMenu(),      typeof(OscSubMenuViewModelBase)) },
        { "Extras", new(Color.FromUInt32(0x_FFFFC6FF), () => new ExtrasSubMenu(),   typeof(ExtrasSubMenuViewModelBase)) },
        { "Debug",  new(Color.FromUInt32(0x_FFFFFFFC), () => new DebugSubMenu(),    typeof(DebugSubMenuViewModelBase)) },
    };

    private readonly ILogger _logger;
    private readonly IContainerBulkLoader<ViewModelBase> _vmLoader;
    private readonly ConfigModel _config;
    private readonly PopupWindowFactory _popup;
    private readonly IBackToFrontNotifyService _notify;

    public CoreMenuViewModelImpl
    (
        ILogger logger,
        IContainerBulkLoader<ViewModelBase> vmLoader, 
        ConfigModel config,
        PopupWindowFactory popup,
        IBackToFrontNotifyService notify
    )
    {
        _logger = logger.ForContext<CoreMenuViewModelImpl>();
        _vmLoader = vmLoader;
        _config = config;
        _popup = popup;
        _notify = notify;

        _logger.Information("Loading buttons...");
        foreach(var buttonInfo in _buttonInfos)
        {
            var navButton = new NavigationButton()
            {
                Title = buttonInfo.Key,
                Color = new(buttonInfo.Value.Color),
            };
            NavButtons?.Add(navButton);
        }

        _notify.OnNotificationSent += HandleNotification;
    }

    #region Notifications
    private void HandleNotification(object? _, BackToFrontNotifyEventArgs e)
    {
        if (e.Level >= BackToFrontNotifyLevel.Error)
        {
            _popup.OpenNotification(e.Title, e.Content, true, true);
        } 
        else
        {
            BannerMessage = $"{e.Title}: {e.Content}";
            BannerColorAccent = e.Level > BackToFrontNotifyLevel.Info;
            BannerVisible = true;
        }
    }
    #endregion

    #region Menu
    private bool _firstLoad = true;
    public override void OnMenuSelected(ListBox listBox)
    {
        if (NavButtons.Count != listBox.Items.Count)
        {
            _logger.Warning("List of items does not align with NavButtons");
            return;
        }

        var selectedIndex = listBox.SelectedIndex;

        for(var i = 0; i < NavButtons.Count; i++)
        {
            NavButtons[i].Selected = i == selectedIndex;
        }

        if (_firstLoad)
        {
            _firstLoad = false;
        }
        else
        {
            ConfigModelLoader.TrySave(_config, PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
        }

        var title = NavButtons[selectedIndex].Title;
        if (!_buttonInfos.TryGetValue(title, out var info))
        {
            _logger.Error("Failed to locate page infos for title {title}", title);
            return;
        }

        var viewModel = _vmLoader.GetInstance(info.ControlType);
        if (!viewModel.IsOk) return;

        var control = info.ControlGenerator();
        control.DataContext = viewModel.Value;
        control.Background = new SolidColorBrush(Colors.Transparent);
        CurrentSubmenu = control;

        Application.Current!.Resources["AccentBrush"] = info.Color;
        Application.Current!.Resources["AccentHalfOpaBrush"] = new Color((byte)(info.Color.A / 2), info.Color.R, info.Color.G, info.Color.B);
    }
    #endregion
}

#if DEBUG
public class CoreMenuViewModelPreview : CoreMenuViewModelBase
{
    public CoreMenuViewModelPreview()
    {
        NavButtons = [
            new() { Title = "Test1", Color = new(Colors.Red)},
            new() { Title = "Test2", Color = new(Colors.Green)},
            new() { Title = "Test3", Color = new(Colors.Blue)}
        ];
        BannerVisible = true;
    }
    
    public override void OnMenuSelected(ListBox listBox)
    {
        
    }
}
#endif