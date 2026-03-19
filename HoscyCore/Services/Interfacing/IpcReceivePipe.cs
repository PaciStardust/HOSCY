using System.IO.Pipes;
using Serilog;

namespace HoscyCore.Services.Interfacing;

public class IpcReceivePipe(ILogger logger, string handle) : IpcPipeBase<AnonymousPipeClientStream>(logger.ForContext<IpcReceivePipe>())
{
    private readonly string _handle = handle;
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

            _logger.Verbose("Received \"{output}\" via IPC", output);
            OnDataReceived.Invoke(output);
        }
    }
}