using HoscyCore.Services.Core;

namespace HoscyCore.Services.Output.Core;

public interface IOutputManagerService : IAutoStartStopService
{
    #region Info
    public IReadOnlyList<IOutputHandlerStartInfo> GetHandlerInfos(bool activeOnly);
    public ServiceStatus GetProcessorStatus(IOutputHandlerStartInfo handlerInfo);
    #endregion

    #region Control
    public void RefreshHandlers();
    public void RestartHandlers();
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
    public event EventHandler<OutputMessageEventArgs> OnMessage;
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