using HoscyCore.Ipc;
using HoscyCore.Services.Recognition.Extra;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace HoscyWhisperV2Process;

public class Program
{   
    private static ConsoleDataWriter _writer = null!;
    private static WhisperIpcConfig _config = null!;
    private static ILogger _logger = null!;

    private static IpcDataConverter? _ipcConverter;
    private static IpcReceivePipe? _pipe;
    private static KeepAliveTimer? _keepAlive;

    public static async Task Main(string[] args)
    {
        if (!InitConfigAndWriter(args)) return;

        var factory = new RecognitionComponentFactory(_config);
        _logger = factory.CreateLogger(_writer);

        if (!string.IsNullOrWhiteSpace(_config.ParentSendingPipe))
        {
            _ipcConverter = new(_logger);

            _pipe = new IpcReceivePipe(_logger, _config.ParentSendingPipe);
            _pipe.OnDataReceived += HandleIpcData;
            _pipe.Start();

            _keepAlive = new(_logger, TimeSpan.FromSeconds(10));
        }

        _writer.SendStatus(true);

        using var audioEngine = factory.CreateAudioEngine(_logger);
        using var capture = factory.CreateCaptureDevice(audioEngine, _logger);
        using var vad = factory.CreateVad(_logger);
        var audioProcessor = factory.CreateAudioProcessor(vad);
        using var whisperProcessor = factory.CreateWhisperProcessor(_logger);

        using var recCore = new WhisperRecognitionCore(whisperProcessor, audioProcessor, capture, _logger);

        using var cts = new CancellationTokenSource();
        var task = Task.Run(async () => await recCore.RecognizeAsync(cts.Token, HandleRecognitionOutput));

        var wait = 250 / 5;
        while(!recCore.IsRunning && !cts.IsCancellationRequested && !task.IsCompleted && wait-- > 0)
        {
            await Task.Delay(5);
        }
        
        _keepAlive?.OnKeepAliveFailed += cts.Cancel;
        _keepAlive?.OnKeepAliveSend += (x) => _writer.SendKeepAlive(x);
        _keepAlive?.Start();

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
            _logger.Error("Recognition task is unexpectedly not running");
        }

        if (task.Exception is not null)
        {
            _logger.Error(task.Exception, "Listening task encountered an error");
        }

        if (_keepAlive is not null)
        {
            _keepAlive.Stop();
            _keepAlive.Dispose();
            _keepAlive = null;
        }

        _writer.SendStatus(false);
    }

    private static bool InitConfigAndWriter(string[] args)
    {
        if (args.Length == 0)
        {
            _writer = new(false);
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

    private static void HandleIpcData(string data)
    {
        if (_ipcConverter is null)
        {
            _logger.Warning("Unable to handle IPC data {data}, converter missing", data);
            return;
        }
        if (!_ipcConverter.IsValid(data))
        {
            _logger.Warning("Unable to handle IPC data {data}, it is invalid", data);
            return;
        }

        var id = _ipcConverter.GetIdentifier(data);
        switch (id)
        {
            case WhisperIpcKeepalive.IDENTIFIER:
                _keepAlive?.TriggerKeepAlive();
                return;

            default: 
                _logger.Warning("Received unknown data with identifier {id}: \"{data}\"", id, data);
                return;
        }
    }
}