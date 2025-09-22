using System;
using System.Linq;
using Hoscy.Configuration.Modern;
using Serilog;
using Serilog.Core;

namespace Hoscy.Utility;

public static class LogUtils
{
    private const string LOGGING_TEMPLATE = "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    public static string LogFileName => _logFileName;
    private static readonly string _logFileName = $"log-{DateTime.Now:MM-dd-yyyy-HH-mm-ss}.txt";


    public static ILogger CreateTemporaryLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFileName, outputTemplate: LOGGING_TEMPLATE)
            .WriteTo.Console(outputTemplate: LOGGING_TEMPLATE)
            .CreateLogger().ForContext<Program>();
    }
    public static Logger CreateLoggerFromConfiguration(ConfigModel config)
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
}