using System;

namespace Hoscy.Services.Output.Core;

public interface IOutputProcessor
{
    #region Information
    /// <summary>
    /// Get information associated with this processor
    /// </summary>
    public OutputProcessorInfo GetInfo();
    /// <summary>
    /// Event that triggers when the process encounteres an Exception outside of starting and stopping
    /// </summary>
    public event EventHandler<Exception> OnRuntimeError;
    #endregion

    #region Start / Stop
    public void Activate();
    public void Shutdown();
    public void Restart();
    public void IsActive();
    #endregion

    #region Functionality
    public bool SendMessage(string contents);
    public bool SendNotification(string contents, OutputNotificationPriority priority);
    public bool Clear();
    public bool SetProcessingIndicator(bool isProcessing);
    #endregion
}