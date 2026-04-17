using HoscyCore.Utility;

namespace HoscyCore.Services.Core;

public interface IStartStopModule : IStartStopService
{
    /// <summary>
    /// Event that triggers when the process encounteres an Exception outside of starting and stopping
    /// </summary>
    public event EventHandler<ResMsg> OnRuntimeError;
    /// <summary>
    /// Event gets called when the processor is stopped
    /// </summary>
    public event EventHandler OnModuleStopped;
}