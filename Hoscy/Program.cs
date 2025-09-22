using Avalonia;
using Hoscy.Models;
using Hoscy.Models.Config;
using Hoscy.Models.Config.Migration;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;

namespace Hoscy;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var tempLogger = CreateTemporaryLogger();

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

        if (config.Logger_OpenWindowOnStartupWindowsOnly)
        {
            Utils.OpenConsoleOnWindows();
        }
        var newLogger = CreateLoggerFromConfiguration(config);
        newLogger.ForContext<Program>().Information("Logger now using config");

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

    private const string LOGGING_TEMPLATE = "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    private const string LOGGING_FILE = "log.txt";
    private static ILogger CreateTemporaryLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.File(LOGGING_FILE, outputTemplate: LOGGING_TEMPLATE)
            .WriteTo.Console(outputTemplate: LOGGING_TEMPLATE)
            .CreateLogger().ForContext<Program>();
    }
    private static Logger CreateLoggerFromConfiguration(ConfigModel config)
    {
        var logConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LOGGING_FILE, outputTemplate: LOGGING_TEMPLATE)
            .MinimumLevel.ControlledBy(config.Logger_MinimumSeverityGetSwitch())
            .Filter.ByExcluding(x =>
            {
                var message = x.RenderMessage();
                return config.Logger_Filters.Any(f => f.Matches(message));
            });

        if (config.Logger_LogToCommandLine)
        {
            logConfig.WriteTo.Console(outputTemplate: LOGGING_TEMPLATE);
        }

        return logConfig.CreateLogger();
    } 
}
