using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyAvaloniaUi.Services.ChangeTracking;

[LoadIntoDiContainer(typeof(TransUnappliedTracker))]
public class TransUnappliedTracker(ITranslationManagerService manager, ILogger logger) 
    : SettingsUnappliedTrackerBase<ITranslationManagerService>(manager, logger.ForContext<TransUnappliedTracker>())
{
    protected override bool CanSetChange(ITranslationManagerService service)
    {
        return service.GetCurrentModuleStatus() != ServiceStatus.Stopped;
    }

    protected override void SubscribeToResetEvent(ITranslationManagerService service)
    {
        service.OnModuleStatusChanged += OnModuleStatusChanged;
    }

    private void OnModuleStatusChanged(ServiceStatus status)
    {
        if (status == ServiceStatus.Stopped)
        {
            ResetChange();
        }
    }
}