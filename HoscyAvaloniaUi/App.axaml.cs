using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HoscyCore.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyAvaloniaUi.Views.Core;
using Serilog;
using HoscyCore;
using Avalonia.Logging;
using HoscyAvaloniaUi.Utility;

namespace HoscyAvaloniaUi;

public partial class App : Application
{
    private readonly HoscyCoreApp _coreApp;
    private ILogger _startLogger;


    public App()
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
        _startLogger.Information("Initializing Avalonia");
        AvaloniaXamlLoader.Load(this);
        _startLogger.Debug("Initializing Avalonia complete");
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
        // DisableAvaloniaDataAnnotationValidation();

        StartApplication(desktop);

        base.OnFrameworkInitializationCompleted();
    }

    // private void DisableAvaloniaDataAnnotationValidation()
    // {
    //     // Get an array of plugins to remove
    //     var dataValidationPluginsToRemove =
    //         BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

    //     // remove each entry found
    //     foreach (var plugin in dataValidationPluginsToRemove)
    //     {
    //         BindingPlugins.DataValidators.Remove(plugin);
    //     }
    // }

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

        Action<ILogger> onNewLoggerLoaded = new((logger) =>
        {
            _startLogger = logger.ForContext<App>();
            _startLogger.Debug("New logger received in startup");
            Logger.Sink = new SerilogAvaloniaSink(_startLogger);
        });

        var startParams = new HoscyCoreAppStartParameters()
        {
            OnProgress = onProgressAction,
            OnNewLoggerCreated = onNewLoggerLoaded
        };

        var startRes = ResC.TWrap(() => _coreApp.Start(startParams), "Failed to start core app", _startLogger, ResMsgLvl.Fatal);
        if (startRes.IsOk)
        {
            onProgressAction.Invoke("Switching to main UI");

            //todo: [FEAT] Display of startup errors
            Dispatcher.UIThread.Invoke(() =>
            {
                mainWindowModel.CurrentView = new MainMenu()
                {
                    DataContext = new MainMenuViewModel()
                };
            });

            return;
        }

        _startLogger.Fatal("Failed starting Services in background ({result})", startRes);
        onProgressAction.Invoke($"Unable to load: {startRes}");
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        var res = ResC.Wrap(_coreApp.Stop, "Failed to shut down core app correctly", _startLogger, ResMsgLvl.Fatal);
        res.IfFail((x) => _startLogger.Error("Failed to shut down core app correctly ({result})", x));
    }
}