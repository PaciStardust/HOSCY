using System.Collections.Generic;
using System.Reflection;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class ManageServicesWindowViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial int SelectedIndex { get; set; } = 0;
    [ObservableProperty]
    public partial bool SelectedIsStartStop { get; set; }
    [ObservableProperty]
    public partial bool SelectedCanStartStop { get; set; }
    [ObservableProperty]
    public partial bool SelectedHasError { get; set; }
    [ObservableProperty]
    public partial string SelectedName { get; set; }
    [ObservableProperty]
    public partial string SelectedRepresents { get; set; }
    [ObservableProperty]
    public partial string SelectedStatus { get; set; }
    [ObservableProperty]
    public partial string SelectedError { get; set; }

    [ObservableProperty]
    public partial List<string> DataDisplayed { get; set; } = [];

    public virtual void SelectionChanged() { }
    public virtual void RefreshClicked() { }
    public virtual void RestartClicked() { }
    public virtual void StartStopClicked() { }
    public virtual void CopyClicked(IClipboard? clipboard) { }
}

[LoadIntoDiContainer(typeof(ManageServicesWindowViewModelBase), Lifetime.Transient)]
public class ManageServicesWindowViewModelImpl : ManageServicesWindowViewModelBase
{
    private readonly ILogger _logger;
    private readonly IService[] _dataInternal;
    private readonly PopupWindowFactory _popup;

    public ManageServicesWindowViewModelImpl
    (
        ILogger logger,
        IBackToFrontNotifyService notify,
        IContainerBulkLoader<IService> serviceLoader,
        PopupWindowFactory popup
    )
    {
        _logger = logger.ForContext<ManageServicesWindowViewModelImpl>();
        _popup = popup;

        var serviceRes = serviceLoader.GetInstances();
        if (!serviceRes.IsOk)
        {
            _logger.Warning("Failed to load services ({msg})", serviceRes.Msg);
            notify.SendResult("Failed to Load Services", serviceRes.Msg);
            _dataInternal = [];
        } 
        else
        {
            _dataInternal = [.. serviceRes.Value];
        }
        UpdateServiceList(0);
        SelectionChanged();
    }

    private void UpdateServiceList(int index)
    {
        DataDisplayed = [];
        foreach (var service in _dataInternal)
        {
            if (service is IStartStopService startStopService)
            {
                var status = startStopService.GetCurrentStatus() switch
                {
                    ServiceStatus.Processing => "P",
                    ServiceStatus.Started => "S",
                    ServiceStatus.Faulted => "F",
                    ServiceStatus.Stopped => "X",
                    _ => "?"
                };
                DataDisplayed.Add($"[{status}] {startStopService.GetType().Name}");
            }
            else
            {
                DataDisplayed.Add($"[?] {service.GetType().Name}");
            }
        }
        SelectedIndex = index.MinMax(-1, _dataInternal.Length - 1);
    }

    public override void SelectionChanged()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Length - 1);

        if (SelectedIndex == -1)
        {
            SelectedError = "[No Service Selected]";
            SelectedHasError = false;
            SelectedIsStartStop = false;
            SelectedName = "[No Service Selected]";
            SelectedRepresents = "[No Service Selected]";
            SelectedStatus = "[No Service Selected]";
            SelectedCanStartStop = false;
            return;
        }

        var selectedItem = _dataInternal[SelectedIndex];
        var selectedType = selectedItem.GetType();

        SelectedName = selectedType.Name;

        var attrib = selectedType.GetCustomAttribute<LoadIntoDiContainerAttribute>();
        if (attrib is not null)
        {
            SelectedRepresents = attrib.AsType.Name;
        }
        else
        {
            SelectedRepresents = selectedType.Name;
        }

        if (selectedItem is IStartStopService startStopItem)
        {
            SelectedCanStartStop = selectedItem is not IStartStopModule;
            SelectedIsStartStop = true;
            SelectedStatus = startStopItem.GetCurrentStatus().ToString();
            
            var err = startStopItem.GetErrorMessageIfExists();
            SelectedHasError = err is not null;
            SelectedError = err?.ToString() ?? "[No Error]";
        }
        else
        {
            SelectedCanStartStop = false;
            SelectedIsStartStop = false;
            SelectedStatus = "[Not StartStop]";
            SelectedHasError = false;
            SelectedError = "[Not StartStop]";
        }
    }

    public override void RefreshClicked()
    {
        UpdateServiceList(SelectedIndex);
    }

    public override void RestartClicked()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Length - 1);
        if (SelectedIndex == -1) return;

        var selected = _dataInternal[SelectedIndex];
        if (selected is not IStartStopService service || selected is IStartStopModule) return;

        var serviceName = service.GetType().Name;
        _logger.Information("Manually restarting Service {serviceName}", serviceName);
        if (service.GetCurrentStatus() != ServiceStatus.Stopped)
        {
            var stopRes = service.Stop();
            if (!stopRes.IsOk)
            {
                _logger.Error("Failed to restart service {service} ({res})", serviceName, stopRes);
                _popup.OpenNotification("Failed to Stop Service", stopRes.Msg.Message, true, true);
                UpdateServiceList(SelectedIndex);
                return;
            }
        }

        var startRes = service.Start();
        if (!startRes.IsOk)
        {
            _logger.Error("Failed to restart service {service} ({res})", serviceName, startRes);
            _popup.OpenNotification("Failed to Start Service", startRes.Msg.Message, true, true);
            UpdateServiceList(SelectedIndex);
            return;
        }

        UpdateServiceList(SelectedIndex);
    }

    public override void StartStopClicked()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Length - 1);
        if (SelectedIndex == -1) return;

        var selected = _dataInternal[SelectedIndex];
        if (selected is not IStartStopService service || selected is IStartStopModule) return;

        var serviceName = service.GetType().Name;
        if (service.GetCurrentStatus() != ServiceStatus.Stopped)
        {
            _logger.Information("Manually stopping service {serviceName}", serviceName);
            var res = service.Stop();
            if (!res.IsOk)
            {
                _logger.Error("Failed to stop service {service} ({res})", serviceName, res);
                _popup.OpenNotification("Failed to Stop Service", res.Msg.Message, true, true);
            }
        }
        else
        {
            _logger.Information("Manually starting service {serviceName}", serviceName);
            var res = service.Start();
            if (!res.IsOk)
            {
                _logger.Error("Failed to start service {service} ({res})", serviceName, res);
                _popup.OpenNotification("Failed to Start Service", res.Msg.Message, true, true);
            }
        }

        UpdateServiceList(SelectedIndex);
    }

    public override void CopyClicked(IClipboard? clipboard)
    {
        _logger.Debug("Received clipboard copy request");
        
        if (clipboard is null)
        {
            _logger.Warning("Clipboard copy request failed, no clipboard available");
            return;
        }

        var res = ResC.WrapR(clipboard.SetTextAsync(SelectedError).AsSync, "Clipboard copy failed", _logger);
        if (res.IsOk)
        {
            _logger.Debug("Clipboard copy request succeeded");
        }
    }
}

#if DEBUG
public class ManageServicesWindowViewModelPreview : ManageServicesWindowViewModelBase
{
    
}
#endif