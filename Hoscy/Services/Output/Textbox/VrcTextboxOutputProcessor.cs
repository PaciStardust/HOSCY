using System;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Output.Core;

namespace Hoscy.Services.Output.Textbox;

[LoadIntoDiContainer(typeof(VrcTextboxOutputProcessor), Lifetime.Transient)]
public class VrcTextboxOutputProcessor : IOutputProcessor
{
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnShutdownCompleted = delegate { };

    public void Activate()
    {
        throw new NotImplementedException();
    }

    public bool Clear()
    {
        throw new NotImplementedException();
    }

    public Exception? GetFaultIfExists()
    {
        throw new NotImplementedException();
    }

    public OutputProcessorInfo GetInfo()
    {
        throw new NotImplementedException();
    }

    public StartStopStatus GetStatus()
    {
        throw new NotImplementedException();
    }

    public bool IsRunning()
    {
        throw new NotImplementedException();
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public bool SendMessage(string contents)
    {
        throw new NotImplementedException();
    }

    public bool SendNotification(string contents, OutputNotificationPriority priority)
    {
        throw new NotImplementedException();
    }

    public bool SetProcessingIndicator(bool isProcessing)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }
}