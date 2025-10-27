using System;

namespace Hoscy.Services.DependencyCore;

public interface IStartStopSubmodule<Tidentifier> : IStartStopService
{
    /// <summary>
    /// Event that triggers when the process encounteres an Exception outside of starting and stopping
    /// </summary>
    public event EventHandler<Exception> OnRuntimeError;
    /// <summary>
    /// Event gets called when the processor is stopped
    /// </summary>
    public event EventHandler OnSubmoduleStopped;
    public Tidentifier GetIdentifier();
}