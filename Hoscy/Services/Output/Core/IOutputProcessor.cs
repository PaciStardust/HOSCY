using System;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Output.Core;

public interface IOutputProcessor : IStartStopSubmodule<OutputProcessorInfo>
{
    #region Functionality
    public void ProcessMessage(string contents);
    public void ProcessNotification(string contents, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion
}