using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Utility;
using Hoscy.ViewModels;
using Hoscy.Views;
using Serilog;

namespace Hoscy;

public partial class App(DiContainer container) : Application
{
    private readonly DiContainer _container = container;
    private readonly ILogger _logger = container.GetRequiredService<ILogger>().ForContext<App>();
    private readonly ConfigModel _config = container.GetRequiredService<ConfigModel>();

    public override void Initialize()
    {
        _logger.Information("Initializing Avalonia...");
        AvaloniaXamlLoader.Load(this);
        _logger.Information("Initializing Avalonia complete");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            //todo: initialize services here?
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger.Information("Shutting down Hoscy...");
        _config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
        try
        {
            _container.StopServices();
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Unable to stop Services correctly");
        }
    }
}