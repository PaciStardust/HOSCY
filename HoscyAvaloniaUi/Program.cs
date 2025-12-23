using Avalonia;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using HoscyAvaloniaUi.Utility;
using Serilog;
using System;

namespace HoscyAvaloniaUi;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        ILogger currentLogger = AvaloniaLogUtils.CreateTemporaryLogger();
        ConfigModel? config = null;
        DiContainer? container = null;

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

            container = DiContainer.LoadFromAssembly(currentLogger, config);

            return BuildAvaloniaApp(container, currentLogger)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                currentLogger.ForContext<Program>().Fatal(ex, "An unexpected and uncaught error has occured, Hoscy will now shut down.");
                config?.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, currentLogger);
                container?.StopServices();
            }
            catch { }
            return -1;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(DiContainer container, ILogger logger)
    {
        return AppBuilder.Configure(() => new App(container))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToSerilog(logger);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
    }
}
