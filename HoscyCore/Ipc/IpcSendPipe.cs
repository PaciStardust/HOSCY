using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Ipc;

public class IpcSendPipe(ILogger logger, bool logVerboseExtra) : IpcPipeBase<AnonymousPipeServerStream>(logger.ForContext<IpcSendPipe>())
{
    private readonly bool _logVerboseExtra = logVerboseExtra;

    protected override AnonymousPipeServerStream CreatePipe()
        => new(PipeDirection.Out, HandleInheritability.Inheritable);

    private readonly ConcurrentQueue<string> _ipcQueue = [];

    public bool IsPipeConnected
        => _ipcPipe.IsConnected;

    public bool CanEnqueue
        => !_isDisposed && _shouldThreadRun && _ipcThread.IsAlive;

    public string GetPipeClientHandle()
        => _ipcPipe.GetClientHandleAsString();

    public bool Enqueue<T>(char id, T message)
    {
        if (!CanEnqueue)
        {
            _logger.Warning("Unable to queue new item of type {type}, not running", typeof(T).Name);
            return false;
        }

        try
        {
            var serialized = JsonConvert.SerializeObject(message);
            var queueMessage = $"{id} {serialized}";
            if (_logVerboseExtra)
            {
                _logger.Verbose("Adding \"{queueItem}\" to IPC send queue", queueMessage);
            }
            _ipcQueue.Enqueue(queueMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to convert type {type} to JSON", typeof(T).Name);
            return false;
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
                if (_logVerboseExtra)
                {
                    _logger.Verbose("Sending \"{queueItem}\" via IPC", messageToSend);
                }
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