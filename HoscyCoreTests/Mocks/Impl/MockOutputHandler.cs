using HoscyCore.Services.Output.Core;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public abstract class MockOutputHandler : MockStartStopModuleBase, IOutputHandler
{
    public required string Name { get; set; }
    public required OutputsAsMediaFlags OutputTypeFlags { get; set; }

    public int ClearCount { get; private set; } = 0; 
    public void Clear()
    {
        ClearCount++;
    }

    public required OutputTranslationFormat TranslationFormat { get; set; }
    public OutputTranslationFormat GetTranslationOutputMode()
        => TranslationFormat;

    public List<string> ReceivedMessages { get; init; } = [];
    public Task HandleMessage(string contents)
    {
        ReceivedMessages.Add(contents);
        return Task.CompletedTask;
    }

    public List<(string Message, OutputNotificationPriority Priority)> ReceivedNotifications { get; init; } = [];
    public Task HandleNotification(string contents, OutputNotificationPriority priority)
    {
        ReceivedNotifications.Add((contents, priority));
        return Task.CompletedTask;
    }

    public List<bool> ReceivedIndicatorStates { get; init; } = [];
    public void SetProcessingIndicator(bool isProcessing)
    {
        ReceivedIndicatorStates.Add(isProcessing);
    }

    public override void ResetStats()
    {
        base.ResetStats();
        ClearCount = 0;
        ReceivedMessages.Clear();
        ReceivedNotifications.Clear();
        ReceivedIndicatorStates.Clear();
    }
}

public class MockOutputHandlerA : MockOutputHandler;
public class MockOutputHandlerB : MockOutputHandler;
public class MockOutputHandlerC : MockOutputHandler;
public class MockOutputHandlerD : MockOutputHandler;