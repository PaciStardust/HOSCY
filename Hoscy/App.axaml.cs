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

public partial class App : Application
{
    private readonly DiContainer _container;
    private readonly ILogger _logger;
    private readonly ConfigModel _config;

    public App()
    {
        _logger = new LoggerConfiguration().CreateLogger();
        _config = new();
        _container = DiContainer.Empty();
    }

    public App(DiContainer container)
    {
        _container = container;
        _logger = container.GetRequiredService<ILogger>().ForContext<App>();
        _config = container.GetRequiredService<ConfigModel>();
    }

    public override void Initialize()
    {
        _logger.Information("Initializing Avalonia...");
        AvaloniaXamlLoader.Load(this);
        _logger.Information("Initializing Avalonia complete");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        DisableAvaloniaDataAnnotationValidation();


        desktop.ShutdownRequested += OnShutdownRequested;
        //todo: intermediary loading window
        _container.StartServices();

        desktop.MainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel()
            {
                CurrentView = new SplashScreen()
                {
                    DataContext = new SplashScreenViewModel()
                    {
                        VersionText = LaunchUtils.GetVersion()
                    }
                }
            },
        };

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
        _logger.Information("Hoscy has shut down, goodnight!");
    }
}