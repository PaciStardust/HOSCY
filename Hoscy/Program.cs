using Avalonia;
using Hoscy.Configuration.Modern;
using Hoscy.Utility;
using Serilog.Core;
using System;

namespace Hoscy;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var tempLogger = LogUtils.CreateTemporaryLogger();
        ConfigModel? config = LaunchUtils.LoadConfigModel(tempLogger);
        if (config is null)
        {
            Utils.ShowErrorBoxOnWindows($"HOSCY encountered an error while loading the configuration file, check the most recent log file for more information");
            return 1;
        }

        if (config.Logger_OpenWindowOnStartupWindowsOnly)
        {
            Utils.OpenConsoleOnWindows();
        }

        var newLogger = LogUtils.CreateLoggerFromConfiguration(config);
        newLogger.ForContext<Program>().Information("Logger now using config");

        //Config.BackupFile(Utils.PathConfigFile);
        //ShutdownMode = ShutdownMode.OnMainWindowClose;

        //Osc.RecreateListener(); //This also loads the config
        //Media.StartMediaDetection();
        //Updater.CheckForUpdates();

        /*
        protected override void OnExit(ExitEventArgs e)
        {
            Running = false;
            if (Recognition.IsRunning)
                Recognition.StopRecognizer();
            Config.SaveConfig();
        }

        //Error handling for unhandled errors
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Config.SaveConfig();
                Logger.Error(e.Exception, "A fatal error has occured, Hoscy will now shut down.");
            }
            catch { }
            Current.Shutdown(-1);
            Environment.Exit(-1);
        }
        */

        return BuildAvaloniaApp(newLogger)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(Logger logger)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToSerilog(logger);
}
