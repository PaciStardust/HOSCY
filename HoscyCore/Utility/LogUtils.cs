using HoscyCore.Configuration.Modern;
using Serilog;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for creating a logger and routing logging
/// </summary>
public static class LogUtils
{
    public const string LOGGING_TEMPLATE = "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    public static string LogFileName => _logFileName;
    private static readonly string _logFileName = $"log-{DateTimeOffset.UtcNow:MM-dd-yyyy-HH-mm-ss}.txt";

    public static ILogger CreateTemporaryLogger<T>(bool disableConsoleLogging = false)
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFileName, outputTemplate: LOGGING_TEMPLATE);

        if (!disableConsoleLogging)
        {
            logConfig.WriteTo.Console(outputTemplate: LOGGING_TEMPLATE);
        }
            
        return logConfig.CreateLogger().ForContext<T>();
    }

    public static Serilog.Core.Logger CreateLoggerFromConfiguration(ConfigModel config, bool disableConsoleLogging = false)
    {
        var logConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFileName, outputTemplate: LOGGING_TEMPLATE)
            .MinimumLevel.ControlledBy(config.Logger_MinimumSeverityGetSwitch())
            .Filter.ByExcluding(x =>
            {
                var message = x.RenderMessage();
                return config.Logger_Filters.Any(f => f.Matches(message));
            });

        if (config.Logger_LogToCommandLine && !disableConsoleLogging)
        {
            logConfig.WriteTo.Console(outputTemplate: LOGGING_TEMPLATE);
        }

        return logConfig.CreateLogger();
    }
}