using System.IO.Pipes;
using HoscyCore.Services.Recognition.Extra;
using Newtonsoft.Json;
using Serilog.Events;

namespace HoscyWhisperV2Process;

public class Program
{   
    private static ConsoleDataWriter _writer = null!;
    private static WhisperIpcConfig _config = null!;
    private static Thread? _pipeThread = null;

    public static async Task Main(string[] args)
    {
        if (!InitConfigAndWriter(args)) return;

        if (!string.IsNullOrWhiteSpace(_config.ParentSendingPipe))
        {

            //todo: create pipe here for handling and keepalive
        }

        _writer.SendStatus(true);

        var factory = new RecognitionComponentFactory(_config);
        var logger = factory.CreateLogger(_writer);
        using var audioEngine = factory.CreateAudioEngine(logger);
        using var capture = factory.CreateCaptureDevice(audioEngine, logger);
        using var vad = factory.CreateVad(logger);
        var audioProcessor = factory.CreateAudioProcessor(vad);
        using var whisperProcessor = factory.CreateWhisperProcessor(logger);

        using var recCore = new WhisperRecognitionCore(whisperProcessor, audioProcessor, capture, logger);

        using var cts = new CancellationTokenSource();
        var task = Task.Run(async () => await recCore.RecognizeAsync(cts.Token, HandleRecognitionOutput));

        var wait = 250 / 5;
        while(!recCore.IsRunning && !cts.IsCancellationRequested && !task.IsCompleted && wait-- > 0)
        {
            await Task.Delay(5);
        }

        if (recCore.IsRunning && !cts.IsCancellationRequested && !task.IsCompleted)
        {
            if (args.Length == 0)
            {
                capture.SetListening(true);
                Console.ReadLine();
                capture.SetListening(false);
                cts.Cancel();
            }

            await task;
        }
        else
        {
            logger.Error("Recognition task is unexpectedly not running");
        }

        if (task.Exception is not null)
        {
            logger.Error(task.Exception, "Listening task encountered an error");
        }

        _writer.SendStatus(false);
    }

    private static bool InitConfigAndWriter(string[] args)
    {
        if (args.Length == 0)
        {
            _writer = new(true);
            _writer.SendLog(LogEventLevel.Information, "Starting process without args, likely running independent");
            _config = new WhisperIpcConfig()
            {
                Whisper_ModelPath = Console.ReadLine()!
            };
        }
        else
        {
            _writer = new(true);
            _writer.SendLog(LogEventLevel.Information, "Starting process args, starting...");
            try
            {
                var bytes = Convert.FromBase64String(args[0]);
                var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                _config = JsonConvert.DeserializeObject<WhisperIpcConfig>(decoded)!;
            }
            catch(Exception ex)
            {
                _writer.SendLog(LogEventLevel.Error, $"{ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private static void HandleRecognitionOutput(WhisperIpcRecognition args)
    {
        _writer.SendRecognized(args);
    }
}