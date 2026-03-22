using System.Diagnostics.CodeAnalysis;
using HoscyCore.Ipc;
using HoscyCore.Services.Recognition.Extra;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace HoscyWhisperV2Process;

public class Program
{   
    private static IpcDataHandler? _ipcDataHandler;
    private static IpcReceivePipe? _pipe;
    private static KeepAliveTimer? _keepAlive;

    public static async Task Main(string[] args)
    {
        if (!InitConfigAndWriter(args, out var writer, out var config)) return;

        writer.SendLog(LogEventLevel.Debug, "Config loaded, entering logic");
        try
        {
            await DoLogic(writer, config);
        }
        catch (Exception ex)
        {
            writer.SendLog(LogEventLevel.Error, $"Logic stopped due to Exception of type {ex.GetType().Name}: {ex.Message}", ex.StackTrace);
        }
        writer.SendLog(LogEventLevel.Debug, "Logic left, shutting down");

        HandleShutdown(writer);
    }

    private static async Task DoLogic(ConsoleDataWriter writer, WhisperIpcConfig config)
    {
        var factory = new RecognitionComponentFactory(config);
        var logger = factory.CreateLogger(writer);

        CreateIpcClassesIfNeeded(config.ParentSendingPipe, logger);

        writer.SendStatus(true);

        using var audioEngine = factory.CreateAudioEngine(logger);
        using var capture = factory.CreateCaptureDevice(audioEngine, logger);
        _ipcDataHandler?.OnMute += (x) =>
        {
            capture.SetListening(x.State);
            writer.SendMute(capture.IsListening);
        };

        using var vad = factory.CreateVad(logger);
        var audioProcessor = factory.CreateAudioProcessor(vad);
        using var whisperProcessor = factory.CreateWhisperProcessor(logger);

        using var recCore = new WhisperRecognitionCore(whisperProcessor, audioProcessor, capture, logger);
        using var cts = new CancellationTokenSource();
        var recognitionTask = Task.Run(async () => await recCore.RecognizeAsync(cts.Token, writer.SendRecognized));
        _ipcDataHandler?.OnStatus += (x) =>
        {
            if (x.State) return;
            logger.Information("Recognition stop signal received");
            cts.Cancel();
        };

        logger.Debug("Waiting for recognition to start");
        var taskRunningWaitSteps = 250 / 5;
        while(!recCore.IsRunning && !cts.IsCancellationRequested && !recognitionTask.IsCompleted && taskRunningWaitSteps > 0)
        {
            taskRunningWaitSteps--;
            await Task.Delay(5);
        }
        
        StartKeepAliveIfNeeded(cts, writer);

        if (recCore.IsRunning && !cts.IsCancellationRequested && !recognitionTask.IsCompleted)
        {
            if (_pipe is null)
            {
                capture.SetListening(true);
                Console.ReadLine();
                capture.SetListening(false);
                cts.Cancel();
            }
            logger.Debug("Waiting for recognition to end");
            await recognitionTask;
        }
        else
        {
            logger.Error("Recognition task is unexpectedly not running");
        }

        if (recognitionTask.Exception is not null)
        {
            logger.Error(recognitionTask.Exception, "Listening task encountered an error");
        }
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

    private static void CreateIpcClassesIfNeeded(string pipeHandle, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(pipeHandle))
        {
            logger.Debug("No pipe handle found, not initializing IPC");
            return;
        }

        logger.Debug("Creating IPC classes");

        _ipcDataHandler = new IpcDataHandler(logger);

        _pipe = new IpcReceivePipe(logger, pipeHandle);
        _pipe.OnDataReceived += _ipcDataHandler.Handle;
        _pipe.Start();

        _keepAlive = new(logger, TimeSpan.FromSeconds(10));
        _ipcDataHandler.OnMute += (_) => _keepAlive.TriggerKeepAlive();

        logger.Debug("IPC classes created");
    }

    private static void StartKeepAliveIfNeeded(CancellationTokenSource cts, ConsoleDataWriter writer)
    {
        if (_keepAlive is null) return;
        
        _keepAlive.OnKeepAliveFailed += cts.Cancel;
        _keepAlive.OnKeepAliveSend += writer.SendKeepAlive;
        _keepAlive.Start();
    }

    private static void HandleShutdown(ConsoleDataWriter writer)
    {
        if (_keepAlive is not null)
        {
            _keepAlive.Stop();
            _keepAlive.Dispose();
            _keepAlive = null;
        }

        writer.SendStatus(false);

        _pipe?.Stop();
        _pipe?.Dispose();
    }
}