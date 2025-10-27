using System;

namespace Hoscy.Services.DependencyCore;

public interface IStartStopSubmodule<Tidentifier> : IStartStopService
{
    public event EventHandler<Exception> OnRuntimeError;
    public event EventHandler OnShutdownCompleted;
    public Tidentifier GetIdentifier();
}