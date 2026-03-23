using System.IO.Pipes;
using Serilog;

namespace HoscyCore.Ipc;

public class IpcReceivePipe(ILogger logger, string handle, bool logVerboseExtra) : IpcPipeBase<AnonymousPipeClientStream>(logger.ForContext<IpcReceivePipe>())
{
    private readonly string _handle = handle;
    private readonly bool _logVerboseExtra = logVerboseExtra;
    public event Action<string> OnDataReceived = delegate { };

    public bool IsPipeConnected
        => _ipcPipe.IsConnected;

    protected override AnonymousPipeClientStream CreatePipe()
        => new(PipeDirection.In, _handle);

    protected override void DoDisposeCleanup() { }
    protected override void DoStopCleanup() { }

    protected override void ThreadLoopInternal()
    {
        using var sr = new StreamReader(_ipcPipe);
        while (ShouldLoopContinue())
        {
            if (!_ipcPipe.IsConnected)
            {
                DelayLoop();
                continue;
            }

            var output = sr.ReadLine();
            if (output is null)
            {
                DelayLoop();
                continue;
            }

            if (_logVerboseExtra) {
                _logger.Verbose("Received \"{output}\" via IPC", output);
            }
            OnDataReceived.Invoke(output);
        }
    }

    public void ClearAction()
    {
        OnDataReceived = delegate { };
    }
}