using Avalonia;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Utility;
using Serilog;
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
        ILogger currentLogger = LogUtils.CreateTemporaryLogger();
        ConfigModel? config = null;

        try
        {
            currentLogger.Warning("Starting HOSCY Version {hoscyVersion}", LaunchUtils.GetVersion());

            config = LaunchUtils.LoadConfigModel(currentLogger);
            if (config is null)
            {
                Utils.ShowErrorBoxOnWindows($"HOSCY encountered an error while loading the configuration file, check the most recent log file for more information");
                return 1;
            }

            if (config.Logger_OpenWindowOnStartupWindowsOnly)
            {
                Utils.OpenConsoleOnWindows();
            }

            currentLogger = LogUtils.CreateLoggerFromConfiguration(config);
            currentLogger.ForContext<Program>().Information("Logger now using config");

            return BuildAvaloniaApp(currentLogger, config)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                currentLogger.Fatal(ex, "An unexpected and uncaught error has occured, Hoscy will now shut down.");
                config?.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, currentLogger);
            }
            catch { }
            return -1;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(ILogger logger, ConfigModel config)
        => AppBuilder.Configure(() => new App(logger, config))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToSerilog(logger);
}
