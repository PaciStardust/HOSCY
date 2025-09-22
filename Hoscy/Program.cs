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
