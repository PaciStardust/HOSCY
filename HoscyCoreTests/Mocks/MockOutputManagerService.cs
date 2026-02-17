using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks;

public class MockOutputManagerService : MockStartStopServiceBase, IOutputManagerService
{
    public event EventHandler<OutputMessageEventArgs> OnMessage = delegate { };
    public event EventHandler<OutputNotificationEventArgs> OnNotification = delegate { };
    public event EventHandler OnClear = delegate { };
    public event EventHandler<bool> OnProcessingIndicatorSet = delegate { };

    public List<(string Message, OutputSettingsFlags Flags)> Messages { get; set; } = [];
    public List<(string Message, OutputSettingsFlags Flags, OutputNotificationPriority Prio)> Notifications { get; set; } = [];
    public Exception? Fault { get; set; } = null;
    public bool Running { get; private set; } = false;
    public bool ProcessingIndicator { get; private set; } = false;

    public void Clear()
    {
        Messages.Clear();
        Notifications.Clear();
        OnClear.Invoke(this, EventArgs.Empty);
    }

    public void SendMessage(string contents, OutputSettingsFlags settings)
    {
        Messages.Add((contents, settings));
        OnMessage.Invoke(this, new(contents, [], null));
    }

    public void SendNotification(string contents, OutputNotificationPriority priority, OutputSettingsFlags settings)
    {
        Notifications.Add((contents, settings, priority));
        OnNotification.Invoke(this, new(contents, [], priority));
    }

    public void SetProcessingIndicator(bool isProcessing)
    {
        ProcessingIndicator = isProcessing;
        OnProcessingIndicatorSet.Invoke(this, isProcessing);
    }

    public override void Start()
    {
        base.Start();
        Clear();
    }

    public override void Stop()
    {
        Clear();
        base.Stop();
    }

    public IReadOnlyList<IOutputHandlerStartInfo> GetHandlerInfos(bool _)
    {
        return [];
    }

    public ServiceStatus GetProcessorStatus(IOutputHandlerStartInfo _)
    {
        return ServiceStatus.Processing;
    }

    public void RefreshHandlers()
    {
        return;
    }

    public void RestartHandlers()
    {
        return;
    }
}