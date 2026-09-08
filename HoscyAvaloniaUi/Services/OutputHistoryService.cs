using System;
using System.Collections.Generic;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(OutputHistoryService), Lifetime.Singleton)]
public class OutputHistoryService(ILogger logger, IOutputManagerService output) : StartStopServiceBase(logger.ForContext<OutputHistoryService>())
{
    private readonly IOutputManagerService _output = output;

    private readonly List<(string Message, string? Translation, string[] Output)> _messageHistory = [];
    private readonly List<(string Message, string[] Output, OutputNotificationPriority Priority)> _notificationHistory = [];
    public bool LastMessagePostClear { get; private set; } = true;
    public bool LastNotificationPostClear { get; private set; } = true;
    private bool _started = false;

    public event Action OnOutputClear = delegate { };
    public event Action<string, string[], OutputNotificationPriority> OnOutputNotification = delegate { };
    public event Action<string, string?, string[]> OnOutputMessage = delegate { };

    protected override bool UseAlreadyStartedProtection => true;

    protected override void DisposeCleanup()
    {
        _messageHistory.Clear();
        _notificationHistory.Clear();
        LastMessagePostClear = true;
        LastNotificationPostClear = true;
        _started = false;
    }

    protected override bool IsProcessing() => _started;
    protected override bool IsStarted() => _started;

    protected override Res StartForService()
    {
        DisposeCleanup();
        _output.OnClear += OnClear;
        _output.OnMessage += OnMessage;
        _output.OnNotification += OnNotification;
        return ResC.Ok();
    }

    protected override Res StopForService()
    {
        _output.OnClear -= OnClear;
        _output.OnMessage -= OnMessage;
        _output.OnNotification -= OnNotification;
        return ResC.Ok();
    }

    private void OnMessage(object? sender, OutputMessageEventArgs e)
    {
        _messageHistory.Add((e.Contents, e.Translation, e.Outputs));
        if (_messageHistory.Count > 20)
        {
            _messageHistory.RemoveAt(0);
        }
        LastMessagePostClear = true;
        OnOutputMessage.Invoke(e.Contents, e.Translation, e.Outputs);
    }

    private void OnNotification(object? sender, OutputNotificationEventArgs e)
    {
        _notificationHistory.Add((e.Contents, e.Outputs, e.Priority));
        if (_notificationHistory.Count > 20)
        {
            _notificationHistory.RemoveAt(0);
        } 
        LastNotificationPostClear = true;
        OnOutputNotification.Invoke(e.Contents, e.Outputs, e.Priority);
    }

    private void OnClear(object? sender, EventArgs e)
    {
        LastMessagePostClear = false;
        LastNotificationPostClear = false;
        OnOutputClear.Invoke();
    }
}