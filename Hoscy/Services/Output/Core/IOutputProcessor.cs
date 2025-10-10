using System;
using Hoscy.Services.DependencyCore;

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
    /// <summary>
    /// Event gets called when the processor is stopped
    /// </summary>
    public event EventHandler OnShutdownCompleted;
    #endregion

    #region Start / Stop
    public void Activate();
    public void Shutdown();
    public void Restart();
    public bool IsRunning();
    public Exception? GetFaultIfExists();
    public StartStopStatus GetStatus();
    #endregion

    #region Functionality
    public bool ProcessMessage(string contents);
    public bool ProcessNotification(string contents, OutputNotificationPriority priority);
    public bool Clear();
    public bool SetProcessingIndicator(bool isProcessing);
    #endregion
}