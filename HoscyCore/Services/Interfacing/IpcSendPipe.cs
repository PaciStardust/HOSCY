using System.Collections.Concurrent;
using System.IO.Pipes;
using HoscyCore.Services.Core;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Services.Interfacing;

public class IpcSendPipe : IDisposable
{
    private readonly AnonymousPipeServerStream _ipcPipe;
    private readonly Thread _ipcThread;
    private readonly ConcurrentQueue<string> _ipcQueue = [];
    private readonly ILogger _logger;
    private bool _isDisposed = false;
    private bool _shouldThreadRun = false;

    public IpcSendPipe(ILogger logger)
    {
        _ipcPipe = new(PipeDirection.Out, HandleInheritability.Inheritable);
        _ipcThread = new(IpcSendLoop)
        {
            IsBackground = true
        };
        _logger = logger;
    }

    public bool IsPipeConnected
        => _ipcPipe.IsConnected;

    public string GetPipeClientHandle()
        => _ipcPipe.GetClientHandleAsString();

    public void Enqueue<T>(char id, T message)
    {
        if (_isDisposed || !_shouldThreadRun || !_ipcThread.IsAlive)
        {
            _logger.Warning("Unable to queue new item of type {type}, not running", typeof(T).Name);
            return;
        }

        try
        {
            var serialized = JsonConvert.SerializeObject(message);
            var queueMessage = $"{id} {serialized}";
            _logger.Verbose("Adding \"{queueItem}\" to IPC send queue", queueMessage);
            _ipcQueue.Enqueue(queueMessage);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to convert type {type} to JSON", typeof(T).Name);
        }
    }

    public void Start()
    {
        if (_shouldThreadRun || _ipcThread.IsAlive) return;
        _shouldThreadRun = true;

        _ipcThread.Start();
        Thread.Sleep(10);
        if (!_ipcThread.IsAlive)
        {
            _shouldThreadRun = false;
            _ipcThread.Join();
            _logger.Error("IPC thread failed to start");
            throw new StartStopServiceException($"IPC thread failed to start");
        }
    }

    public void Stop()
    {
        if (!_shouldThreadRun && !_ipcThread.IsAlive) return;
        _ipcQueue?.Clear();
        _shouldThreadRun = false;
        try
        {
            _ipcThread.Join(50);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop IPC thread");
        }
    }

    private void IpcSendLoop()
    {
        if (_isDisposed || !_shouldThreadRun) return;

        _logger.Information("Entering IPC loop");
        using var sw = new StreamWriter(_ipcPipe) { AutoFlush = true };
        while (!_isDisposed && _shouldThreadRun)
        {
            if (!_ipcPipe.IsConnected || _ipcQueue.Count == 0 || !_ipcQueue.TryDequeue(out var messageToSend))
            {
                Thread.Sleep(10);
                continue;
            }

            try
            {
                _logger.Verbose("Sending \"{queueItem}\" via IPC", messageToSend);
                sw.WriteLine(messageToSend);
                _ipcPipe.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to send message {message} to process", messageToSend);
            }
        }
        _logger.Information("Leaving IPC loop");
    }

    public void Dispose()
    {
        _isDisposed = true;
        Stop();
        _ipcPipe.DisposeLocalCopyOfClientHandle();
        _ipcPipe.Dispose();
    }
}