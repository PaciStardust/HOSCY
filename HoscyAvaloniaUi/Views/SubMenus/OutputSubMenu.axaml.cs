using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class OutputSubMenu : UserControl
{
    public OutputSubMenu()
    {
        InitializeComponent();
    }

    private void ReplacementsPartialClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ReplacementsPartialClicked();
        e.Handled = true;
    }
    private void ReplacementsFullClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ReplacementsFullClicked();
        e.Handled = true;
    }

    private void ModuleReloadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleReloadClicked();
        e.Handled = true;
    }
    private void ModuleRestartClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleRestartClicked();
        e.Handled = true;
    }
    private void ModuleToggled(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleToggled();
        e.Handled = true;
    }

    private void ModuleApiEditPresets(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiEditPresets();
        e.Handled = true;
    }
    private void ModuleApiPresetMessageChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox.Message);
        e.Handled = true;
    }
    private void ModuleApiPresetNotificationChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox.Notification);
        e.Handled = true;
    }
    private void ModuleApiPresetClearChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox.Clear);
        e.Handled = true;
    }
    private void ModuleApiPresetProcessingChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox.Processing);
        e.Handled = true;
    }
    private void ModuleApiTranslationFormatChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as OutputSubMenuViewModelBase)?.ModuleApiTranslationFormatChanged();
        e.Handled = true;
    }
}