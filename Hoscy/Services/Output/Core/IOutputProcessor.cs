using System;

namespace Hoscy.Services.Output.Core;

public interface IOutputProcessor
{
    public void Activate();
    public void Shutdown();
    public void Restart();
    public void IsActive();
    public OutputProcessorInfo GetInfo();
    public event Action<Exception> OnRuntimeError;
    public bool SendMessage(string contents);
    public bool SendNotification(string contents, OutputNotificationPriority priority);
    public bool Clear();
    public bool SetProcessingIndicator(bool isProcessing);
}