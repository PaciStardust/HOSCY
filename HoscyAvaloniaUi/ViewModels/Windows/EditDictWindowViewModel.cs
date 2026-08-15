using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditDictWindowViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Dictionary Editor";
    [ObservableProperty]
    public partial string KeyHeader { get; set; } = "Key";
    [ObservableProperty]
    public partial string KeyPlaceholder { get; set; } = "Key...";
    [ObservableProperty]
    public partial string KeySelected { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ValueHeader { get; set; } = "Value";
    [ObservableProperty]
    public partial string ValuePlaceholder { get; set; } = "Value...";
    [ObservableProperty]
    public partial string ValueSelected { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int IndexSelected { get; set; } = 0;

    [ObservableProperty]
    public partial List<string> DataDisplayed { get; set; } = [];
    protected Dictionary<string, string> _dataInternal = [];

    public virtual void Init(string title, string keyName, string valyeName, Dictionary<string, string> dict) { }
    protected virtual void RefreshDiplayList(int index) { }
    public virtual void AddOrModifyEntry() { }
    public virtual void KeyPressed(KeyEventArgs args) { }
    public virtual void RemoveEntry() { }
    public virtual void SelectionChanged() { }
}

[LoadIntoDiContainer(typeof(EditDictWindowViewModelBase), Lifetime.Transient)]
public class EditDictWindowViewModelImpl(ILogger logger) : EditDictWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditDictWindowViewModelImpl>();

    public override void Init(string title, string keyName, string valueName, Dictionary<string, string> dict)
    {
        _logger.Debug("Initializing dictionary editor with title {title}", title);
        Title = title;
        KeyHeader = keyName;
        KeyPlaceholder = keyName + " ...";
        ValueHeader = valueName;
        ValuePlaceholder = valueName + " ...";
        _dataInternal = dict;
        RefreshDiplayList(0);
    }

    protected override void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in _dataInternal) {
            newValues.Add($"{x.Key} : {x.Value}");
        }
        DataDisplayed = newValues;
        IndexSelected = index.MinMax(-1, _dataInternal.Count - 1);
    }

    public override void SelectionChanged()
    {
        IndexSelected = IndexSelected.MinMax(-1, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            KeySelected = string.Empty;
            ValueSelected = string.Empty;
            return;
        }

        var curKey = _dataInternal.Keys.ToArray()[IndexSelected];
        KeySelected = curKey;
        ValueSelected = _dataInternal[curKey];
    }

    public override void AddOrModifyEntry()
    {
        var newIndex = IndexSelected;
        if (_dataInternal.ContainsKey(KeySelected))
        {
            _dataInternal[KeySelected] = ValueSelected;
        }
        else
        {
            _dataInternal.Add(KeySelected, ValueSelected);
            newIndex = _dataInternal.Count - 1;
        }
        _logger.Debug("Created or updated value for key {key} in dictionary editor {title}",
            KeySelected, Title);
        RefreshDiplayList(newIndex);
    }

    public override void RemoveEntry()
    {
        IndexSelected = IndexSelected.MinMax(-1, _dataInternal.Count - 1);
        if (_dataInternal.Count == 0 || IndexSelected == -1)
        {
            return;
        }
        _dataInternal.Remove(_dataInternal.Keys.ToArray()[IndexSelected]);
        _logger.Debug("Removed value for key {key} in dictionary editor {title}",
            KeySelected, Title);
        RefreshDiplayList(IndexSelected - 1);
    }

    public override void KeyPressed(KeyEventArgs args)
    {
        if (args.Key == Key.Enter)
        {
            AddOrModifyEntry();
        }
    }
}

#if DEBUG
public class EditDictWindowViewModelPreview : EditDictWindowViewModelBase
{
    
}
#endif