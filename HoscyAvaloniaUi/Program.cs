using Avalonia;
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
        ILogger currentLogger = LogUtils.CreateTemporaryLogger<Program>();
        try
        {
            return BuildAvaloniaApp(currentLogger)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                currentLogger.ForContext<Program>().Fatal(ex, "An unexpected and uncaught error has occured, Hoscy will now shut down.");
            }
            catch { }
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(ILogger logger)
    {
        return AppBuilder.Configure(() => new App(logger))
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
