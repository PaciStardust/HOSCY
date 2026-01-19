using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Output.Core;

public interface IOutputManagerService : IAutoStartStopService
{
    #region Info
    /// <summary>
    /// Retrieves information about all or only active processors
    /// </summary>
    public IReadOnlyList<OutputProcessorInfo> GetInfos(bool activeOnly);
    #endregion

    #region Start / Stop
    public void ActivateProcessor(OutputProcessorInfo info);
    public void ShutdownProcessor(OutputProcessorInfo info);
    public void RestartProcessor(OutputProcessorInfo info);
    public ServiceStatus GetProcessorStatus(OutputProcessorInfo info);
    #endregion

    #region Processor Control
    public void SendMessage(string contents, OutputSettingsFlags settings);
    public void SendNotification(string contents, OutputNotificationPriority priority, OutputSettingsFlags settings);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion

    #region Events
    /// <summary>
    /// Event gets called whenever a message gets processed
    /// </summary>
    public event EventHandler<(string, string?)> OnMessage;
    /// <summary>
    /// Event gets called whenever a notification gets processed
    /// </summary>
    public event EventHandler<OutputNotificationEventArgs> OnNotification;
    /// <summary>
    /// Event gets called whenever clear is called
    /// </summary>
    public event EventHandler OnClear;
    /// <summary>
    /// Event gets called whenever the processing indicator is set
    /// </summary>
    public event EventHandler<bool> OnProcessingIndicatorSet;
    #endregion
}