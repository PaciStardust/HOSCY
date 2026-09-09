using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Afk;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Recognition.Core;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class InfoSubMenuViewModelBase : ViewModelBase
{
    protected const string TXT_MSG_NOTHING_SENT = "No message sent since opening";
    protected const string TXT_NOT_NOTHING_SENT = "No notification sent since opening";
    protected const string TXT_CLEARED = "[CLEARED]";

    [ObservableProperty]
    public partial IBrush ListeningStatusBrush { get; set; } = new SolidColorBrush(Colors.HotPink);
    [ObservableProperty]
    public partial string ListeningStatusText { get; set; } = "Muted";

    [ObservableProperty]
    public partial IBrush ActiveStatusBrush { get; set; } = new SolidColorBrush(Colors.HotPink);
    [ObservableProperty]
    public partial string ActiveStatusText { get; set; } = "Stopped";

    [ObservableProperty]
    public partial string SentViaText { get; set; } = TXT_MSG_NOTHING_SENT;
    [ObservableProperty]
    public partial string MessageText { get; set; } = TXT_MSG_NOTHING_SENT;
    [ObservableProperty]
    public partial string NotificationText { get; set; } = TXT_NOT_NOTHING_SENT;

    public virtual void ButtonToggleListeningClicked() { }
    public virtual void ButtonClearClicked() { }
    public virtual void ButtonStartStopClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(InfoSubMenuViewModelBase), Lifetime.Transient)]
public class InfoSubMenuViewModelImpl : InfoSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly UiHelperService _uiHelper;
    private readonly PopupWindowFactory _popup;
    private readonly IRecognitionManagerService _recognition;
    private readonly IOutputManagerService _output;
    private readonly OutputHistoryService _outputHistory;
    private readonly IAfkService _afk;

    public InfoSubMenuViewModelImpl
    (
        ILogger logger,
        UiHelperService uiHelper,
        PopupWindowFactory popup,
        IRecognitionManagerService recognition,
        IOutputManagerService output,
        OutputHistoryService outputHistory,
        IAfkService afk
    )
    {
        _logger = logger.ForContext<InfoSubMenuViewModelImpl>();
        _uiHelper = uiHelper;
        _popup = popup;
        _recognition = recognition;
        _output = output;
        _outputHistory = outputHistory;
        _afk = afk;

        UpdateRecognitionStatus(_recognition.IsListening, _recognition.GetCurrentModuleStatus());
        _recognition.OnModuleStatusChanged += OnRecognitionModuleStatusChanged;

        UpdateLastSentInfo();
        _outputHistory.OnOutputUpdate += UpdateLastSentInfo;
    }

    private void OnRecognitionModuleStatusChanged(object? sender, RecognitionStatusChangedEventArgs e)
    {
        UpdateRecognitionStatus(e.IsListening, e.Status);
    }
    private void UpdateRecognitionStatus(bool listening, ServiceStatus status)
    {
        (ListeningStatusBrush, ListeningStatusText) = listening 
            ? (_uiHelper.ValidBrush, "Listening")
            : (_uiHelper.InvalidBrush, "Muted");
        (ActiveStatusBrush, ActiveStatusText) = status == ServiceStatus.Stopped
            ? (_uiHelper.InvalidBrush, "Stopped")
            : (_uiHelper.ValidBrush, "Running");
    }

    public override void ButtonStartStopClicked()
    {
        if (_recognition.GetCurrentModuleStatus() == ServiceStatus.Stopped)
        {
            _logger.Information("Starting recognition module");
            ListeningStatusText = "Starting";
            var res = _recognition.StartModule();
            res.IfFail(x => _popup.OpenNotification("Failed to start recognition module", x.Message, true, true));
        } 
        else
        {
            _logger.Information("Stopping recognition module");
            ListeningStatusText = "Stopping";
            var res = _recognition.StopModule();
            res.IfFail(x => _popup.OpenNotification("Failed to stop recognition module", x.Message, true, true));
        }
    }
    public override void ButtonToggleListeningClicked()
    {
        if (_recognition.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            var value = !_recognition.IsListening;
            _logger.Information("Setting recognition listening status to {value}", value);
            var res = _recognition.SetListening(value);
            res.IfFail(x => _popup.OpenNotification("Failed to set recognition module listening status", x.Message, true, true));
        }
    }
    public override void ButtonClearClicked()
    {
        _logger.Information("Performing Clear");
        _output.Clear();
        _afk.StopAfk();
    }

    private void UpdateLastSentInfo()
    {
        if (_outputHistory.LastMessagePostClear)
        {
            var lastMsg = _outputHistory.GetLastMessage();
            SentViaText = lastMsg is null ? TXT_MSG_NOTHING_SENT : $"Sent via {string.Join(", ", lastMsg.Value.Outputs.Length > 0 ? lastMsg.Value.Outputs : ["Nothing"])}";
            MessageText = lastMsg is null ? TXT_MSG_NOTHING_SENT : lastMsg.Value.Translation is null ? lastMsg.Value.Message : $"{lastMsg.Value.Message}\n<=>\n{lastMsg.Value.Translation}";
        }
        else
        {
            SentViaText = TXT_CLEARED;
            MessageText = TXT_CLEARED;
        }
        if (_outputHistory.LastNotificationPostClear)
        {
            var lastNotify = _outputHistory.GetLastNotification();
            NotificationText = lastNotify is null ? TXT_NOT_NOTHING_SENT : $"[{string.Join(", ", lastNotify.Value.Outputs.Length > 0 ?  lastNotify.Value.Outputs : ["Nothing"])}] {lastNotify.Value.Message}";
        }
        else
        {
            NotificationText = TXT_CLEARED;
        }
    }
}

#if DEBUG
public class InfoSubMenuViewModelPreview : InfoSubMenuViewModelBase
{
    
}
#endif