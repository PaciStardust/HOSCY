using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyAvaloniaUi.Views.Core;
using Serilog;
using HoscyCore;

namespace HoscyAvaloniaUi;

public partial class App : Application
{
    private readonly HoscyCoreApp _coreApp;
    private ILogger _startLogger;


    public App() //todo: fix logger
    {
        _startLogger = new LoggerConfiguration().CreateLogger();
        _coreApp = new(_startLogger);
    }

    public App(ILogger logger)
    {
        _startLogger = logger;
        _coreApp = new(_startLogger);
    }

    public override void Initialize()
    {
        _startLogger.Information("Initializing Avalonia...");
        AvaloniaXamlLoader.Load(this);
        _startLogger.Information("Initializing Avalonia complete");
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

        StartApplication(desktop);

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

    private void StartApplication(IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.ShutdownRequested += OnShutdownRequested;

        var splashModel = new SplashScreenViewModel()
        {
            VersionText = LaunchUtils.GetVersion(),
            Progress = "Loading Splash Screen"
        };
        var splashView = new SplashScreen()
        {
            DataContext = splashModel
        };

        var mainWindowModel = new MainWindowViewModel()
        {
            CurrentView = splashView
        };
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainWindowModel
        };

        Task.Run(() => StartApplicationBackgroundTask(mainWindowModel, splashModel));
    }

    private void StartApplicationBackgroundTask(MainWindowViewModel mainWindowModel, SplashScreenViewModel splashModel)
    {
        Action<string> onProgressAction = new((text) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
               splashModel.Progress = text; 
            });
        });

        DiContainer? container = null;
        try
        {
            var startParams = new HoscyCoreAppStartParameters()
            {
                OnProgress = onProgressAction
            };
            _coreApp.Start(startParams);
            container = _coreApp.GetContainer();
            _startLogger = container.GetService<ILogger>()?.ForContext<App>() ?? _startLogger;
            onProgressAction.Invoke("Switching to main UI");
        } catch (Exception ex)
        {
            _startLogger.Fatal(ex, "Failed starting Services in background");
            onProgressAction.Invoke("Unable to load, please check logs");
            try
            {
                _coreApp.Stop();
            } catch (Exception ex2)
            {
                _startLogger.Error(ex2, "Failed to stop Hoscy correcly");
            }
            return;
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            mainWindowModel.CurrentView = new MainMenu()
            {
                DataContext = new MainMenuViewModel()
            };
        });
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _coreApp.Stop();
    }
}