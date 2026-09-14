using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Services.Core;
using Serilog;

namespace HoscyAvaloniaUi.Services.ChangeTracking;

public abstract partial class SettingsUnappliedTrackerBase<T> 
    : ObservableObject, IService where T : IService
{
    private readonly T _service;
    protected readonly ILogger _logger;

    [ObservableProperty]
    public partial bool Unapplied { get; private set; } = false;

    public SettingsUnappliedTrackerBase(T service, ILogger logger)
    {
        _service = service;
        _logger = logger;
        SubscribeToResetEvent(_service);
    }

    public abstract void SubscribeToResetEvent(T service);
    public abstract bool CanSetChange(T service);
    
    protected void ResetChange()
    {
        if (Unapplied)
        {
            _logger.Debug("Resetting unapplied flag");
            Unapplied = false;
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
            }
        }
        else
        {
            ResetChange();
        }
    }
}