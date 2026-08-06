using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditApiPresetsWindowViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial string PresetNameSelected { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string TargetUrlSelected { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string ResultFieldSelected { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int TimeoutMsSelected { get; set; } = 0;
    [ObservableProperty]
    public partial string ContentTypeSelected { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string ContentToSendSelected { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string AuthHeaderSelected { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int IndexSelected { get; set; } = 0;

    [ObservableProperty]
    public partial List<string> DataDisplayed { get; set; } = [];
    protected List<ApiPresetModel> _dataInternal = [];

    public virtual void Init(List<ApiPresetModel> data) { }
    protected virtual void RefreshDiplayList(int index) { }
    public virtual void AddEntry() { }
    public virtual void KeyPressed(KeyEventArgs args) { }
    public virtual void ModifyEntry() { }
    public virtual void RemoveEntry() { }
    public virtual void EditHeaders(Window window) { }
    public virtual void SelectionChanged() { }
}

[LoadIntoDiContainer(typeof(EditApiPresetsWindowViewModelBase), Lifetime.Transient)]
public class EditApiPresetsWindowViewModelImpl
(
    ILogger logger,
    PopupWindowFactory popupFactory
) 
: EditApiPresetsWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditApiPresetsWindowViewModelImpl>();
    private readonly PopupWindowFactory _popupFactory = popupFactory;

    public override void Init(List<ApiPresetModel> data)
    {
        _dataInternal = data;
        RefreshDiplayList(0);
    }

    protected override void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in _dataInternal) {
            newValues.Add(x.Name);
        }
        DataDisplayed = newValues;
        IndexSelected = Math.Min(index, _dataInternal.Count - 1);
    }

    public override void SelectionChanged()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            PresetNameSelected = string.Empty;
            TargetUrlSelected = string.Empty;
            ResultFieldSelected = string.Empty;
            TimeoutMsSelected = 3000;
            ContentTypeSelected = string.Empty;
            ContentToSendSelected = string.Empty;
            AuthHeaderSelected = string.Empty;
            return;
        }

        var selectedItem = _dataInternal[IndexSelected];
        PresetNameSelected = selectedItem.Name;
        TargetUrlSelected = selectedItem.TargetUrl;
        ResultFieldSelected = selectedItem.ResultField;
        TimeoutMsSelected = selectedItem.ConnectionTimeout;
        ContentTypeSelected = selectedItem.ContentType;
        ContentToSendSelected = selectedItem.SentData;
        AuthHeaderSelected = selectedItem.Authorization;
    }

    public override void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(PresetNameSelected))
        {
            return;
        }
        _dataInternal.Add(CreateModel());
        RefreshDiplayList(_dataInternal.Count - 1);
    }

    public override void RemoveEntry()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            return;
        }

        _dataInternal.RemoveAt(IndexSelected);
        RefreshDiplayList(IndexSelected - 1);
    }

    public override void ModifyEntry()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            AddEntry();
            return;
        }

        _dataInternal[IndexSelected] = CreateModel();
        RefreshDiplayList(IndexSelected);
    }

    public override void EditHeaders(Window window)
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            return;
        }

        var data = _dataInternal[IndexSelected];
        _popupFactory.OpenEditDict($"Editing headers for API preset {data.Name}", "Header Name", "Header Value", data.HeaderValues, window);
    }

    public override void KeyPressed(KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;

        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected != -1 && PresetNameSelected == _dataInternal[IndexSelected].Name)
        {
            ModifyEntry();
        }
        else
        {
            AddEntry();
        }
    }

    private ApiPresetModel CreateModel()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);

        var model = new ApiPresetModel();

        if (!string.IsNullOrWhiteSpace(PresetNameSelected))
            model.Name = PresetNameSelected;

        if (!string.IsNullOrWhiteSpace(TargetUrlSelected))
            model.TargetUrl = TargetUrlSelected;

        if (!string.IsNullOrWhiteSpace(ResultFieldSelected))
            model.ResultField = ResultFieldSelected;

        model.ConnectionTimeout = TimeoutMsSelected;

        if (!string.IsNullOrWhiteSpace(ContentToSendSelected))
            model.SentData = ContentToSendSelected;

        if (!string.IsNullOrWhiteSpace(ContentTypeSelected))
            model.ContentType = ContentTypeSelected;

        if (IndexSelected != -1)
            model.HeaderValues = _dataInternal[IndexSelected].HeaderValues
                .ToDictionary(x => x.Key, x => x.Value);

        if (!string.IsNullOrWhiteSpace(AuthHeaderSelected))
            model.Authorization = AuthHeaderSelected;

        return model;
    }
}

#if DEBUG
public class EditApiPresetsWindowViewModelPreview : EditApiPresetsWindowViewModelBase
{
    
}
#endif