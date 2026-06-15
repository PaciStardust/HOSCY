using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoscyAvaloniaUi.Components;
using HoscyAvaloniaUi.Views.SubMenus;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Core;

public abstract partial class CoreMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial List<NavigationButton> NavButtons { get; set; } = [];

    [ObservableProperty]
    public partial UserControl CurrentSubmenu { get; set; } = new();

    [RelayCommand]
    protected abstract void OnNavigationSelected();
}


[LoadIntoDiContainer(typeof(CoreMenuViewModelBase), Lifetime.Transient)]
public partial class CoreMenuViewModel : CoreMenuViewModelBase
{
    private record NavButtonInfo(Color Color, Type ControlType);
    private static readonly Dictionary<string, NavButtonInfo> _buttonInfos = new() {
        { "T1", new(Colors.Red, typeof(SubMenuTest)) }
    };

    private readonly ILogger _logger;
    private readonly IContainerBulkLoader<UserControl> _controlLoader;

    public CoreMenuViewModel(ILogger logger, IContainerBulkLoader<UserControl> controlLoader)
    {
        _logger = logger.ForContext<CoreMenuViewModel>();
        _controlLoader = controlLoader;

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

    protected override void OnNavigationSelected()
    {
        
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
    
    protected override void OnNavigationSelected()
    {
        
    }
}

#endif