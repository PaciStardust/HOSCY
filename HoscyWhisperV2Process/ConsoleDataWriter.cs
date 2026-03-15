using HoscyCore.Services.Recognition.Extra;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;

namespace HoscyWhisperV2Process;

public class ConsoleDataWriter(bool asJson)
{
    private readonly bool _asJson = asJson;

    public void SendLog(LogEventLevel level, string message, string? trace = null)
    {
        if (_asJson)
        {
            var log = new WhisperIpcLog() { LogLevel = level, Message = message, Trace = trace };
            SendAsJson(WhisperIpcLog.IDENTIFIER, log);
        }
        else
        {
            SendLogReadable(level, trace is null ? message : $"{message}\n{trace}");
        }
    } 

    private static void SendLogReadable(LogEventLevel level, string message)
    {
        SendReadable($"{level.ToString()[..3]}: {message}");
    }
    private static void SendReadable(string message)
    {
        Console.WriteLine($"x {message}");
    }

    private static void SendAsJson<T>(char id, T data)
    {
        try
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            Console.WriteLine($"{id} {json}");
        }
        catch (Exception ex)
        {
            SendLogReadable(LogEventLevel.Error, $"JSON Error: {ex.GetType().Name} {ex.Message}");
        }
    }
}

public class ConsoleDataWriterLogSink(ConsoleDataWriter writer) : ILogEventSink
{
    private readonly ConsoleDataWriter _writer = writer;

    public void Emit(LogEvent logEvent)
    {
        _writer.SendLog(logEvent.Level, logEvent.RenderMessage(), logEvent.Exception?.StackTrace);
    }
}

