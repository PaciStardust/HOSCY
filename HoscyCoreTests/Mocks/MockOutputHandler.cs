using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks;

public abstract class MockOutputHandler : MockStartStopServiceBase, IOutputHandler
{
    public required string Name { get; set; }
    public required OutputsAsMediaFlags OutputTypeFlags { get; set; }

    public event EventHandler<Exception> OnRuntimeError = delegate {};
    public event EventHandler OnSubmoduleStopped = delegate {};

    public int ClearCount { get; private set; } = 0; 
    public void Clear()
    {
        ClearCount++;
    }

    public required OutputTranslationFormat TranslationFormat { get; set; }
    public OutputTranslationFormat GetTranslationOutputMode()
        => TranslationFormat;

    public List<string> ReceivedMessages { get; init; } = [];
    public void HandleMessage(string contents)
    {
        ReceivedMessages.Add(contents);
    }

    public List<(string Message, OutputNotificationPriority Priority)> ReceivedNotifications { get; init; } = [];
    public void HandleNotification(string contents, OutputNotificationPriority priority)
    {
        ReceivedNotifications.Add((contents, priority));
    }

    public List<bool> ReceivedIndicatorStates { get; init; } = [];
    public void SetProcessingIndicator(bool isProcessing)
    {
        ReceivedIndicatorStates.Add(isProcessing);
    }

    public void ResetStats()
    {
        _fault = null;
        ClearCount = 0;
        ReceivedMessages.Clear();
        ReceivedNotifications.Clear();
        ReceivedIndicatorStates.Clear();
    }

    public ServiceStatus? OverrideRunningStatus { get; set; } = null;
    public override ServiceStatus GetCurrentStatus()
    {
        if (Started)
        {
            return OverrideRunningStatus ?? ServiceStatus.Processing;
        }
        return ServiceStatus.Stopped;
    }

    public override void Stop()
    {
        base.Stop();
        OnSubmoduleStopped.Invoke(this, EventArgs.Empty);
    }

    private Exception? _fault = null;
    public override Exception? GetFaultIfExists()
    {
        return _fault;
    }
    public void InduceError(Exception? ex)
    {
        _fault = ex;
        if (ex is not null)
        {
            OnRuntimeError.Invoke(this, ex);
        }
    }
}

public class MockOutputHandlerA : MockOutputHandler;
public class MockOutputHandlerB : MockOutputHandler;
public class MockOutputHandlerC : MockOutputHandler;
public class MockOutputHandlerD : MockOutputHandler;