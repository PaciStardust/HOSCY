using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using Serilog;

namespace HoscyAvaloniaUi.Services.ChangeTracking;

[LoadIntoDiContainer(typeof(RecogUnappliedTracker))]
public class RecogUnappliedTracker(IRecognitionManagerService manager, ILogger logger) 
    : SettingsUnappliedTrackerBase<IRecognitionManagerService>(manager, logger.ForContext<RecogUnappliedTracker>())
{
    protected override bool CanSetChange(IRecognitionManagerService service)
    {
        return service.GetCurrentModuleStatus() != ServiceStatus.Stopped;
    }

    protected override void SubscribeToResetEvent(IRecognitionManagerService service)
    {
        service.OnModuleStatusChanged += OnModuleStatusChanged;
    }

    private void OnModuleStatusChanged(object? sender, RecognitionStatusChangedEventArgs e)
    {
        if (e.Status == ServiceStatus.Stopped)
        {
            ResetChange();
        }
    }
}