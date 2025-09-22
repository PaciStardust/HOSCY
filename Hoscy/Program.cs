using Avalonia;
using Hoscy.Configuration.Legacy;
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

        ConfigModel? config;
        try
        {
            tempLogger.Information("Attempting to load config file...");
            config = ConfigModelLoader.Load(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, tempLogger);
            if (config is null)
            {
                tempLogger.Information("Could not find config file, attempting to load legacy config file instead...");
                config = LegacyConfigModelLoader.Load(PathUtils.PathConfigFolder, LegacyConfigModelLoader.DEFAULT_FILE_NAME, tempLogger)?
                .Upgrade(tempLogger)
                .Migrate(tempLogger);
            }
            if (config is null)
            {
                tempLogger.Information("Could not find legacy config file, creating new file insted...");
                config = new();
            }
            config.Upgrade(tempLogger);
            tempLogger.Information("Successfully created and upgraded the provided configuration");
            //todo: backup old?
        }
        catch (Exception ex)
        {
            tempLogger.Fatal(ex, "Program wil shut down - Failed loading config file");
            Utils.ShowErrorBoxOnWindows($"HOSCY encountered a {ex.GetType().Name} while loading the configuration file, check the most recent log file for more information");
            return 1;
        }

        if (config.Logger_OpenWindowOnStartupWindowsOnly)
        {
            Utils.OpenConsoleOnWindows();
        }
        var newLogger = LogUtils.CreateLoggerFromConfiguration(config);
        newLogger.ForContext<Program>().Information("Logger now using config");

        BuildAvaloniaApp(newLogger)
            .StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(Logger logger)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToSerilog(logger);
}
