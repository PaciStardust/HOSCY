using System;
using System.Collections.Generic;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(OutputHistoryService), Lifetime.Singleton)]
public class OutputHistoryService(ILogger logger, IOutputManagerService output) 
    : StartStopServiceBase(logger.ForContext<OutputHistoryService>()), IAutoStartStopService
{
    private readonly IOutputManagerService _output = output;

    private readonly List<(string Message, string? Translation, string[] Output)> _messageHistory = [];
    private readonly List<(string Message, string[] Output, OutputNotificationPriority Priority)> _notificationHistory = [];
    public bool LastMessagePostClear { get; private set; } = true;
    public bool LastNotificationPostClear { get; private set; } = true;
    private bool _started = false;

    public event Action OnOutputUpdate = delegate { };

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
        _started = true;
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
        var contents = e.Contents.Length > 2048 ? e.Contents[2048..] : e.Contents;
        var trans = e.Translation is null ? null : e.Translation.Length > 2048 ? e.Translation[2048..] : e.Translation;
        _messageHistory.Add((contents, trans, e.Outputs));
        if (_messageHistory.Count > 20)
        {
            _messageHistory.RemoveAt(0);
        }
        LastMessagePostClear = true;
        OnOutputUpdate.Invoke();
    }

    private void OnNotification(object? sender, OutputNotificationEventArgs e) //todo: notification source?
    {
        var contents = e.Contents.Length > 512 ? e.Contents[512..] : e.Contents;
        _notificationHistory.Add((contents, e.Outputs, e.Priority));
        if (_notificationHistory.Count > 20)
        {
            _notificationHistory.RemoveAt(0);
        } 
        LastNotificationPostClear = true;
        OnOutputUpdate.Invoke();
    }

    private void OnClear(object? sender, EventArgs e)
    {
        LastMessagePostClear = false;
        LastNotificationPostClear = false;
        OnOutputUpdate.Invoke();
    }

    public (string Message, string? Translation, string[] Outputs)? GetLastMessage()
    {
        return _messageHistory.Count > 0 ? _messageHistory[^1] : null;
    }
    public (string Message, string[] Outputs, OutputNotificationPriority Priority)? GetLastNotification()
    {
        return _notificationHistory.Count > 0 ? _notificationHistory[^1] : null;
    }
}