using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Voice.Core;
using Serilog;

namespace HoscyAvaloniaUi.Services.ChangeTracking;

[LoadIntoDiContainer(typeof(VoiceUnappliedTracker))]
public class VoiceUnappliedTracker(IVoiceManagerService manager, ILogger logger) 
    : SettingsUnappliedTrackerBase<IVoiceManagerService>(manager, logger.ForContext<VoiceUnappliedTracker>())
{
    protected override bool CanSetChange(IVoiceManagerService service)
    {
        return service.GetCurrentModuleStatus() != ServiceStatus.Stopped;
    }

    protected override void SubscribeToResetEvent(IVoiceManagerService service)
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