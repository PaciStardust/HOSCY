using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class VoiceSubMenu : UserControl
{
    public VoiceSubMenu()
    {
        InitializeComponent();
    }

    private void OptionsSelectedModuleChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSelectedModuleChanged();
        e.Handled = true;
    }
    private void OptionsSelectedModuleStartStopClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSelectedModuleStartStopClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSelectedModuleRefreshClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRestartClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSelectedModuleRestartClicked();
        e.Handled = true;
    }

    private void OptionsSpeakerChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSpeakerChanged();
        e.Handled = true;
    }
    private void OptionsSpeakerRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSpeakerRefreshClicked();
        e.Handled = true;
    }

    private void OptionsSpeakerVolumeChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.OptionsSpeakerVolumeChanged();
        e.Handled = true;
    }

    private void ModulesAnyApiEditPresets(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.ModulesAnyApiEditPresets();
        e.Handled = true;
    }
    private void ModulesAnyApiPresetChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.ModulesAnyApiPresetChanged();
        e.Handled = true;
    }

    private void ModulesAzureEditVoices(object? sender, RoutedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.ModulesAzureEditVoices();
        e.Handled = true;
    }
    private void ModulesAzureVoiceChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as VoiceSubMenuViewModelBase)?.ModulesAzureVoiceChanged();
        e.Handled = true;
    }
}