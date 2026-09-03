using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class RecogSubMenu : UserControl
{
    public RecogSubMenu()
    {
        InitializeComponent();
    }

    private void OptionsSelectedModuleChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleChanged();
        e.Handled = true;
    }
    private void OptionsSelectedModuleStartStopClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleStartStopClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleRefreshClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRestartClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleRestartClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleToggleMuteClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleToggleMuteClicked();
        e.Handled = true;
    }

    private void OptionsOutputNoiseFilterClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsOutputNoiseFilterClicked();
        e.Handled = true;
    }

    private void OptionsMicrophoneChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsMicrophoneChanged();
        e.Handled = true;
    }

    private void OptionsMicrophoneRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsMicrophoneRefreshClicked();
        e.Handled = true;
    }

    private void ModulesAnyApiEditPresets(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesAnyApiEditPresets();
        e.Handled = true;
    }
    private void ModulesAnyApiPresetChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesAnyApiPresetChanged();
        e.Handled = true;
    }

    private void ModulesAzureEditPresetPhrases(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesAzureEditPresetPhrases();
        e.Handled = true;
    }
    private void ModulesAzureEditLanguages(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesAzureEditLanguages();
        e.Handled = true;
    }

    private void ModulesVoskEditModels(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesVoskEditModels();
        e.Handled = true;
    }
    private void ModulesVoskModelChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesVoskModelChanged();
        e.Handled = true;
    }

    private void ModulesWhisperEditModels(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesWhisperEditModels();
        e.Handled = true;
    }
    private void ModulesWhisperModelChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesWhisperModelChanged();
        e.Handled = true;
    }
    private void ModulesWhisperEditNoiseFilter(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesWhisperEditNoiseFilter();
        e.Handled = true;
    }
    private void ModulesWhisperVadModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesWhisperVadModeChanged();
        e.Handled = true;
    }

    private void ModulesWindowsModelChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.ModulesWindowsModelChanged();
        e.Handled = true;
    }
}