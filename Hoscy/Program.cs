using Avalonia;
using Hoscy.Models;
using Hoscy.Models.Config;
using Hoscy.Models.Config.Migration;
using Serilog;
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
        var tempLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.File("log.txt", outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger().ForContext<Program>();

        ConfigModel? config;
        try
        {
            tempLogger.Information("Attempting to load config file...");
            config = ConfigModelLoader.Load(Utils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, tempLogger);
            if (config is null)
            {
                tempLogger.Information("Could not find config file, attempting to load legacy config file instead...");
                config = LegacyConfigModelLoader.Load(Utils.PathConfigFolder, LegacyConfigModelLoader.DEFAULT_FILE_NAME, tempLogger)?
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

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
