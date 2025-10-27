using System;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public abstract class OutputProcessorBase : StartStopServiceBase, IOutputProcessor
{
    #region Events
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnShutdownCompleted = delegate { };
    #endregion

    #region Info & Status
    public abstract OutputProcessorInfo GetIdentifier();
    #endregion

    #region Functionality
    public abstract void Clear();
    public abstract void ProcessMessage(string contents);
    public abstract void ProcessNotification(string contents, OutputNotificationPriority priority);
    public abstract void SetProcessingIndicator(bool isProcessing);
    #endregion
}