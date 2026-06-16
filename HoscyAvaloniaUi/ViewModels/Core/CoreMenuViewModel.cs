using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Components;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.SubMenus;
using HoscyAvaloniaUi.Views.SubMenus;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Core;

public abstract partial class CoreMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial List<NavigationButton> NavButtons { get; set; } = [];

    [ObservableProperty]
    public partial UserControl CurrentSubmenu { get; set; } = new();

    public abstract void OnMenuSelected(ListBox listBox);
}


[LoadIntoDiContainer(typeof(CoreMenuViewModelBase), Lifetime.Transient)]
public partial class CoreMenuViewModelImpl : CoreMenuViewModelBase
{
    private record NavButtonInfo(Color Color, Func<UserControl> ControlGenerator, Type ControlType);
    private static readonly Dictionary<string, NavButtonInfo> _buttonInfos = new() {
        { "Info", new(Color.FromUInt32(0x_FFC6FFFF), () => new InfoSubMenu(), typeof(InfoSubMenuViewModelBase)) }
    };

    private readonly ILogger _logger;
    private readonly IContainerBulkLoader<ViewModelBase> _vmLoader;
    private readonly ConfigModel _config;

    public CoreMenuViewModelImpl(ILogger logger, IContainerBulkLoader<ViewModelBase> vmLoader, ConfigModel config)
    {
        _logger = logger.ForContext<CoreMenuViewModelImpl>();
        _vmLoader = vmLoader;
        _config = config;

        _logger.Information("Loading buttons...");
        foreach(var buttonInfo in _buttonInfos)
        {
            var navButton = new NavigationButton()
            {
                Title = buttonInfo.Key,
                Color = new(buttonInfo.Value.Color),
            };
            NavButtons.Add(navButton);
        }
    }

    private bool _firstLoad = true;
    public override void OnMenuSelected(ListBox listBox)
    {
        if (NavButtons.Count != listBox.Items.Count)
        {
            _logger.Warning("List of items does not align with NavButtons");
            return;
        }

        var selectedIndex = listBox.SelectedIndex;

        var baseColor = AvaloniaColorHelper.GetBrush(listBox, "BackgroundBaseBrush");
        for(var i = 0; i < NavButtons.Count; i++)
        {
            if (i != selectedIndex)
            {
                NavButtons[i].Background = baseColor;
            }
        }

        NavButtons[selectedIndex].Background = AvaloniaColorHelper.GetBrush(listBox, "BackgroundLightBrush");

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
        CurrentSubmenu = control;

        Application.Current!.Resources["AccentBrush"] = info.Color;
    }
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
    }
    
    public override void OnMenuSelected(ListBox listBox)
    {
        
    }
}

#endif