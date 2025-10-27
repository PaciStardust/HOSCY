using System;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public interface IOutputProcessor //todo: could be startstop?
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
    public void ProcessMessage(string contents);
    public void ProcessNotification(string contents, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion
}