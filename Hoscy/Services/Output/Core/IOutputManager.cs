using System;
using System.Collections.Generic;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public interface IOutputManager : IStartStopService
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
    public bool IsProcessorActive(OutputProcessorInfo info);
    #endregion

    #region Processor Control
    public bool SendMessage(string contents);
    public bool SendNotification(string contents, OutputNotificationPriority priority);
    public bool Clear();
    public bool SetProcessingIndicator(bool isProcessing);
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
    #endregion
}