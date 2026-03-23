using HoscyCore.Ipc;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog;

namespace HoscyWhisperV2Process;

public class WhisperApp : IDisposable
{
    #region Ctor
    private readonly WhisperIpcConfig _config;
    private readonly ConsoleDataWriter _writer;
    private readonly RecognitionComponentFactory _factory;
    private readonly ILogger _logger;

    public WhisperApp(WhisperIpcConfig config, ConsoleDataWriter writer)
    {
        _config = config;
        _writer = writer;
        _factory = new(config);
        _logger = _factory.CreateLogger(writer);
    }

    private IpcDataHandler? _ipcDataHandler;
    private IpcReceivePipe? _ipcPipe;
    private KeepAliveTimer? _ipcKeepAlive;
    #endregion

    #region Running
    public async Task Run()
    {
        InitIpcClassesIfNeeded();

        using var audioEngine = _factory.CreateAudioEngine(_logger);
        using var capture = _factory.CreateCaptureDevice(audioEngine, _logger);
        _ipcDataHandler?.OnMute += (x) =>
        {
            capture.SetListening(x.State);
            _writer.SendMute(capture.IsListening);
        };

        using var vad = _factory.CreateVad(_logger);
        var audioProcessor = _factory.CreateAudioProcessor(vad);
        using var whisperProcessor = _factory.CreateWhisperProcessor(_logger);

        using var recCore = new WhisperRecognitionCore(whisperProcessor, audioProcessor, capture, _logger);
        using var cts = new CancellationTokenSource();
        var recognitionTask = Task.Run(async () => await recCore.RecognizeAsync(cts.Token, _writer.SendRecognized));
        _ipcDataHandler?.OnStatus += (x) =>
        {
            if (x.State) return;
            _logger.Information("Recognition stop signal received");
            TryCancelCts(cts);
        };

        _logger.Debug("Waiting for recognition to start");
        var res = await OtherUtils.WaitWhileAsync(() => !recCore.IsRunning && !cts.IsCancellationRequested && !recognitionTask.IsCompleted, 4000, 25);

        _writer.SendStatus(true);

        if (res)
        {
            StartKeepAliveIfNeeded(cts);

            if (_ipcPipe is null)
            {
                capture.SetListening(true);
                Console.ReadLine();
                capture.SetListening(false);
                TryCancelCts(cts);
            }
            _logger.Debug("Waiting for recognition to end");
            await recognitionTask;
        }
        else
        {
            _logger.Error("Recognition task is unexpectedly not running");
        }

        if (recognitionTask.Exception is not null)
        {
            _logger.Error(recognitionTask.Exception, "Listening task encountered an error");
        }

        StopIpcDataHandling();
    }
    #endregion

    #region Stopping
    private void TryCancelCts(CancellationTokenSource cts)
    {
        StopIpcDataHandling();
        try
        {
            cts.Cancel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to cancel CTS");
        }
    }

    public void Dispose()
    {
        StopIpcDataHandling();
        
        _ipcDataHandler = null;

        _ipcKeepAlive?.Dispose();
        _ipcKeepAlive = null;

        _ipcPipe?.Stop();
        _ipcPipe?.Dispose();        
        _ipcPipe = null;
    }

    private void StopIpcDataHandling()
    {
        _ipcKeepAlive?.Stop();
        _ipcPipe?.ClearAction();
        _ipcDataHandler?.ClearActions();
    }
    #endregion

    #region IPC
    private void InitIpcClassesIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_config.ParentSendingPipe))
        {
            _logger.Debug("No pipe handle found, not initializing IPC");
            return;
        }

        _logger.Debug("Creating IPC classes");

        _ipcDataHandler = new IpcDataHandler(_logger);

        _ipcPipe = new IpcReceivePipe(_logger, _config.ParentSendingPipe, _config.Debug_LogVerboseExtra);
        _ipcPipe.OnDataReceived += _ipcDataHandler.Handle;
        _ipcPipe.Start();

        _ipcKeepAlive = new(_logger, TimeSpan.FromSeconds(10));
        _ipcDataHandler.OnKeepAlive += (_) => _ipcKeepAlive.TriggerKeepAlive();

        _logger.Debug("IPC classes created");
    }

    private void StartKeepAliveIfNeeded(CancellationTokenSource cts)
    {
        if (_ipcKeepAlive is null) return;
        
        _ipcKeepAlive.OnKeepAliveFailed += () =>
        {
            TryCancelCts(cts);
        };
        _ipcKeepAlive.OnKeepAliveSend += _writer.SendKeepAlive;
        _ipcKeepAlive.Start();
    }
    #endregion
}