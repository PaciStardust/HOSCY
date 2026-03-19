using System.IO.Pipes;
using HoscyCore.Services.Core;
using Serilog;

namespace HoscyCore.Services.Interfacing;

public abstract class IpcPipeBase<T> : IDisposable where T : PipeStream
{
    protected readonly T _ipcPipe;
    protected readonly Thread _ipcThread;
    protected readonly ILogger _logger;
    protected bool _isDisposed = false;
    protected bool _shouldThreadRun = false;

    public IpcPipeBase(ILogger logger)
    {
        _ipcPipe = CreatePipe();
        _ipcThread = new(ThreadLoop)
        {
            IsBackground = true
        };
        _logger = logger;
    }
    protected abstract T CreatePipe();

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
        _shouldThreadRun = false;
        try
        {
            DoStopCleanup();
            _ipcThread.Join(50);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop IPC thread");
        }
    }
    protected abstract void DoStopCleanup();

    private void ThreadLoop()
    {
        if (_isDisposed || !_shouldThreadRun) return;

        _logger.Information("Entering IPC loop");
        ThreadLoopInternal();
        _logger.Information("Leaving IPC loop");
    }
    protected abstract void ThreadLoopInternal();
    protected bool ShouldLoopContinue()
        => !_isDisposed && _shouldThreadRun;
    protected void DelayLoop()
        => Thread.Sleep(10);

    public void Dispose()
    {
        _isDisposed = true;
        Stop();
        DoDisposeCleanup();
        _ipcPipe.Dispose();
    }
    protected abstract void DoDisposeCleanup();
}