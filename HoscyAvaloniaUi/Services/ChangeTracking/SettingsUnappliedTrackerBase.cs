using System;
using HoscyCore.Services.Core;
using Serilog;

namespace HoscyAvaloniaUi.Services.ChangeTracking;

public abstract class SettingsUnappliedTrackerBase<T> : IService where T : IService
{
    private readonly T _service;
    protected readonly ILogger _logger;

    public bool Unapplied { get; private set; } = false;
    public event Action<bool> OnUnappliedChanged = delegate { };

    public SettingsUnappliedTrackerBase(T service, ILogger logger)
    {
        _service = service;
        _logger = logger;
        SubscribeToResetEvent(_service);
    }

    protected abstract void SubscribeToResetEvent(T service);
    protected abstract bool CanSetChange(T service);
    
    protected void ResetChange()
    {
        if (Unapplied)
        {
            _logger.Debug("Resetting unapplied flag");
            Unapplied = false;
            OnUnappliedChanged.Invoke(false);
        }
    }

    public void SetChange()
    {
        if (CanSetChange(_service))
        {
            if (!Unapplied)
            {
                _logger.Debug("Setting unapplied flag");
                Unapplied = true;
                OnUnappliedChanged.Invoke(true);
            }
        }
        else
        {
            ResetChange();
        }
    }
}