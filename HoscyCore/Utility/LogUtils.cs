using System;
using System.Linq;
using Avalonia;
using Avalonia.Logging;
using HoscyCore.Configuration.Modern;
using Serilog;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for creating a logger and routing logging
/// </summary>
public static class LogUtils
{
    private const string LOGGING_TEMPLATE = "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    public static string LogFileName => _logFileName;
    private static readonly string _logFileName = $"log-{DateTimeOffset.UtcNow:MM-dd-yyyy-HH-mm-ss}.txt";


    public static ILogger CreateTemporaryLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFileName, outputTemplate: LOGGING_TEMPLATE)
            .WriteTo.Console(outputTemplate: LOGGING_TEMPLATE)
            .CreateLogger().ForContext<Program>();
    }
    public static Serilog.Core.Logger CreateLoggerFromConfiguration(ConfigModel config)
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

        if (config.Logger_LogToCommandLine)
        {
            logConfig.WriteTo.Console(outputTemplate: LOGGING_TEMPLATE);
        }

        return logConfig.CreateLogger();
    }

    /// <summary>
    /// Route Avalonia Logging to Serilog
    /// </summary>
    public static AppBuilder LogToSerilog(this AppBuilder builder, ILogger logger)
    {
        Logger.Sink = new SerilogAvaloniaSink(logger);
        return builder;
    }
}

/// <summary>
/// Sink to permit Avalonia to log to Serilog
/// </summary>
public class SerilogAvaloniaSink(ILogger logger) : ILogSink
{
    private readonly ILogger _logger = logger.ForContext<SerilogAvaloniaSink>();

    public bool IsEnabled(LogEventLevel level, string area)
    {
        return true;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        var logText = $"{area} {(source is null ? string.Empty : $"({source.GetType()})")} > {messageTemplate}";
        _logger.Write(ConvertLogLevel(level), logText);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        var logText = $"{area} {(source is null ? string.Empty : $"({source.GetType()})")} > {messageTemplate}";
        _logger.Write(ConvertLogLevel(level), logText, propertyValues);
    }

    private static Serilog.Events.LogEventLevel ConvertLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Warning
        };
    }
}