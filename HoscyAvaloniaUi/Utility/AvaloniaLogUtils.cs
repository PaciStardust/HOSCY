using Avalonia;
using Avalonia.Logging;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

/// <summary>
/// Utilities for creating a logger and routing logging
/// </summary>
public static class AvaloniaLogUtils
{
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