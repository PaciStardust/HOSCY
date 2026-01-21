using HoscyCore.Configuration.Modern;
using Serilog;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for creating a logger and routing logging
/// </summary>
public static class LogUtils //todo: [REFACTOR] Use ILogger<T> and refactor log design
{
    public const string LOGGING_TEMPLATE = "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    public static string LogFileName => _logFileName;
    private const string LOG_FILE_START = "log-";
    private const string LOG_FILE_END = ".txt";
    private static readonly string _logFileName = $"{LOG_FILE_START}{DateTimeOffset.UtcNow:MM-dd-yyyy-HH-mm-ss}{LOG_FILE_END}";

    public static ILogger CreateTemporaryLogger<T>(bool disableConsoleLogging = false)
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFileName, outputTemplate: LOGGING_TEMPLATE)
            .WriteTo.Debug(outputTemplate: LOGGING_TEMPLATE);

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
            .WriteTo.Debug(outputTemplate: LOGGING_TEMPLATE)
            .MinimumLevel.ControlledBy(config.Debug_LogMinimumSeverityGetSwitch())
            .Filter.ByExcluding(x =>
            {
                var message = x.RenderMessage();
                return config.Debug_LogFilters.Any(f => f.Matches(message));
            });

        if (config.Debug_LogViaTerminal && !disableConsoleLogging)
        {
            logConfig.WriteTo.Console(outputTemplate: LOGGING_TEMPLATE);
        }

        return logConfig.CreateLogger();
    }

    public static void TryCleanLogs(string directory, ILogger logger)
    {
        logger.Information("Attempting to clean up old logs in directory \"{directory}\"", directory);
        try
        {
            if (!Directory.Exists(directory))
            {
                logger.Warning($"Log directory does not exist: {directory}");
                return;
            }

            var logFilesToDelete = Directory.GetFiles(directory)
                .Where(x => {
                    var fName = Path.GetFileName(x);
                    return fName.StartsWith(LOG_FILE_START) && fName.EndsWith(LOG_FILE_END);
                })
                .Select(x => (x, File.GetCreationTimeUtc(x)))
                .OrderByDescending(x => x.Item2)
                .Skip(5)
                .ToArray();

            if (logFilesToDelete.Length == 0)
            {
                logger.Information("No old log files to delete.");
                return;
            }

            logger.Debug("Deleting {count} old log files");
            foreach(var (file, _) in logFilesToDelete)
            {
                logger.Debug("Deleting old log file: \"{file}\"", file);
                File.Delete(path: file);
                logger.Debug("Deleted old log file: \"{file}\"", file);
            }
            logger.Debug("Deleted {count} old log files");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to clean up old log files");
        }
    }
}