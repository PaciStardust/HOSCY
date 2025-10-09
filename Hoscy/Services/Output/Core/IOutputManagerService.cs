using System;
using System.Collections.Generic;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public interface IOutputManagerService : IStartStopService
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
    public StartStopStatus GetProcessorStatus(OutputProcessorInfo info);
    #endregion

    #region Processor Control
    public void SendMessage(string contents);
    public void SendNotification(string contents, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion

    #region Events
    /// <summary>
    /// Event gets called whenever a message gets processed
    /// </summary>
    public event EventHandler<string> OnMessage;
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