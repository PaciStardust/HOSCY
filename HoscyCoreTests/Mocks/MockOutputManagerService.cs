using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks;

public class MockOutputManagerService : IOutputManagerService
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

    public void ActivateProcessor(OutputProcessorInfo info)
    {
        return;
    }

    public void Clear()
    {
        Messages.Clear();
        Notifications.Clear();
        OnClear.Invoke(this, EventArgs.Empty);
    }

    public ServiceStatus GetCurrentStatus()
    {
        return Running ? (
            GetFaultIfExists() is null
            ? ServiceStatus.Processing
            : ServiceStatus.Faulted
        )
        : ServiceStatus.Stopped;
    }

    public Exception? GetFaultIfExists()
    {
        return Fault;
    }

    public IReadOnlyList<OutputProcessorInfo> GetInfos(bool activeOnly)
    {
        return [];
    }

    public ServiceStatus GetProcessorStatus(OutputProcessorInfo info)
    {
        return ServiceStatus.Stopped;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void RestartProcessor(OutputProcessorInfo info)
    {
        return;
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

    public void ShutdownProcessor(OutputProcessorInfo info)
    {
        return;
    }

    public void Start()
    {
        Clear();
    }

    public void Stop()
    {
        Clear();
    }
}