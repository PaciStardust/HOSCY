using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.Recognition.Extra;
using Newtonsoft.Json;
using Serilog.Events;

namespace HoscyWhisperV2Process;

public class Program //todo: [FIX] Typing indicator not good, noises, encoding, japanese
{   
    public static async Task Main(string[] args) //todo: [REFACTOR?] Should result type see heavier usage here
    {
        if (!InitConfigAndWriter(args, out var writer, out var config)) return;

        writer.SendLog(LogEventLevel.Information, "Config successfully loaded, running app");
        var app = new WhisperApp(config, writer);
        try
        {
            await app.Run();
        }
        catch (Exception ex)
        {
            writer.SendLog(LogEventLevel.Error, $"Logic stopped due to Exception of type {ex.GetType().Name}: {ex.Message}", ex.StackTrace);
        }

        writer.SendLog(LogEventLevel.Information, "App no longer runnning, cleaning up");
        try
        {
            app.Dispose();
        }
        catch (Exception ex)
        {
            writer.SendLog(LogEventLevel.Error, $"Cleanup failed due to Exception of type {ex.GetType().Name}: {ex.Message}", ex.StackTrace);
        }

        writer.SendLog(LogEventLevel.Information, "App shut down, goodnight!");
    }

    private static bool InitConfigAndWriter(string[] args, 
        [NotNullWhen(true)] out ConsoleDataWriter? writer, [NotNullWhen(true)] out WhisperIpcConfig? config)
    {
        if (args.Length == 0)
        {
            writer = new ConsoleDataWriter(false);
            writer.SendLog(LogEventLevel.Information, "Starting process without args, likely running independent");
            config = new WhisperIpcConfig()
            {
                Whisper_ModelPath = Console.ReadLine()!
            };
            return true;
        }
        else
        {
            writer = new ConsoleDataWriter(true);
            writer.SendLog(LogEventLevel.Information, "Starting process with args");
            try
            {
                var bytes = Convert.FromBase64String(args[0]);
                var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                config = JsonConvert.DeserializeObject<WhisperIpcConfig>(decoded)!;
                return true;
            }
            catch(Exception ex)
            {
                writer.SendLog(LogEventLevel.Error, $"{ex.GetType().Name}: {ex.Message}");
                config = null;
                return false;
            }
        }
    }
}