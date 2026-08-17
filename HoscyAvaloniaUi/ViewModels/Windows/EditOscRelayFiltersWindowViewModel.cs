using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditOscRelayFiltersWindowViewModelBase : EditComplexListWindowViewModelBase<OscRelayFilterModel>
{
    [ObservableProperty]
    public partial string SelectedName { get; set; }
    [ObservableProperty]
    public partial ushort SelectedPort { get; set; }
    [ObservableProperty]
    public partial string SelectedIp { get; set; }
    [ObservableProperty]
    public partial bool SelectedBlacklistMode { get; set; }
    [ObservableProperty]
    public partial bool SelectedEnabled { get; set; }

    public virtual void FiltersClicked(Window window) { }
}

[LoadIntoDiContainer(typeof(EditOscRelayFiltersWindowViewModelBase), Lifetime.Transient)]
public class EditOscRelayFiltersWindowViewModelImpl(ILogger logger, PopupWindowFactory popup) : EditOscRelayFiltersWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditOscRelayFiltersWindowViewModelBase>();
    private readonly PopupWindowFactory _popup = popup;

    protected override OscRelayFilterModel CreateModelInternal(OscRelayFilterModel? model)
    {
        var newModel = new OscRelayFilterModel()
        {
            Port = SelectedPort,
            BlacklistMode = SelectedBlacklistMode,
            Enabled = SelectedEnabled,
            Filters = model is null ? [] : model.Filters
        };

        if (!string.IsNullOrWhiteSpace(SelectedName))
        {
            newModel.Name = SelectedName;
        }
        if (!string.IsNullOrWhiteSpace(SelectedIp))
        {
            newModel.Ip = SelectedIp;
        }

        return newModel;
    }

    protected override string GetItemDisplayText(OscRelayFilterModel item)
    {
        return item.ToString();
    }

    protected override string GetModelIdentifier(OscRelayFilterModel selectedModel)
    {
        return selectedModel.Name;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedName;
    }

    protected override void LogModelAdded(OscRelayFilterModel model)
    {
        _logger.Debug("Creating new OSC Relay Filter entry {entry}", model.ToString());
    }

    protected override void LogModelModified(OscRelayFilterModel oldModel, OscRelayFilterModel newModel)
    {
        _logger.Debug("Updating OSC Relay Filter entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(OscRelayFilterModel model)
    {
        _logger.Debug("Removing OSC Relay Filter entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var model = new OscRelayFilterModel();
        SetSelectedDataWithItem(model);
    }

    protected override void SetSelectedDataWithItem(OscRelayFilterModel item)
    {
        SelectedBlacklistMode = item.BlacklistMode;
        SelectedEnabled = item.Enabled;
        SelectedIp = item.Ip;
        SelectedName = item.Name;
        SelectedPort = item.Port;
    }

    public override void FiltersClicked(Window window)
    {
        var selectedModel = GetSelectedModel();
        if (selectedModel is null)
        {
            return;
        }
        _popup.OpenEditList(selectedModel.Filters, $"Editing filters for preset {selectedModel.Name}", "Filter Text", window);
    }
}

#if DEBUG
public class EditOscRelayFiltersWindowViewModelPreview : EditOscRelayFiltersWindowViewModelBase
{
    protected override OscRelayFilterModel CreateModelInternal(OscRelayFilterModel? model)
    {
        return new();
    }
}
#endif