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
        ConfigModel? config = LaunchUtils.LoadConfigModel(tempLogger); //todo: maybe move saving?
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
        */

        try
        {
            return BuildAvaloniaApp(newLogger, config)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                newLogger.Fatal(ex, "An unexpected and uncaught error has occured, Hoscy will now shut down.");
                config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, newLogger);
                //todo: Gracefully stop services if possible?
            }
            catch { }
            return -1;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(Logger logger, ConfigModel config)
        => AppBuilder.Configure(() => new App(logger, config))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToSerilog(logger);
}
