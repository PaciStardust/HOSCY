using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Services.Interfacing;

public class IpcSendPipe(ILogger logger) : IpcPipeBase<AnonymousPipeServerStream>(logger.ForContext<IpcSendPipe>())
{
    protected override AnonymousPipeServerStream CreatePipe()
        => new(PipeDirection.Out, HandleInheritability.Inheritable);

    private readonly ConcurrentQueue<string> _ipcQueue = [];

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

    protected override void DoStopCleanup()
    {
        _ipcQueue.Clear();
    }

    protected override void DoDisposeCleanup()
    {
        _ipcPipe.DisposeLocalCopyOfClientHandle();
    }

    protected override void ThreadLoopInternal()
    {
        using var sw = new StreamWriter(_ipcPipe) { AutoFlush = true };
        while (ShouldLoopContinue())
        {
            if (!_ipcPipe.IsConnected || _ipcQueue.Count == 0 || !_ipcQueue.TryDequeue(out var messageToSend))
            {
                DelayLoop();
                continue;
            }

            try
            {
                _logger.Verbose("Sending \"{queueItem}\" via IPC", messageToSend);
                sw.WriteLine(messageToSend);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _ipcPipe.WaitForPipeDrain();
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to send message {message} to process", messageToSend);
            }
        }
    }
}