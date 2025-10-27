using System;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public abstract class OutputProcessorBase : StartStopSubmoduleBase<OutputProcessorInfo>, IOutputProcessor
{
    #region Functionality
    public abstract void Clear();
    public abstract void ProcessMessage(string contents);
    public abstract void ProcessNotification(string contents, OutputNotificationPriority priority);
    public abstract void SetProcessingIndicator(bool isProcessing);
    #endregion
}