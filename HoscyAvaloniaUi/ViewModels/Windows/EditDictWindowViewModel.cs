using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;
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
    protected Dictionary<string, string> DataInternal { get; set; } = [];

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
        DataInternal = dict;
        RefreshDiplayList(0);
    }

    protected override void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in DataInternal) {
            newValues.Add($"{x.Key} : {x.Value}");
        }
        DataDisplayed = newValues;
        IndexSelected = Math.Min(index, DataInternal.Count - 1);
    }

    public override void SelectionChanged()
    {
        if (IndexSelected == -1)
        {
            KeySelected = string.Empty;
            ValueSelected = string.Empty;
            return;
        }

        IndexSelected = Math.Min(IndexSelected, DataInternal.Count - 1);
        var curKey = DataInternal.Keys.ToArray()[IndexSelected];
        KeySelected = curKey;
        ValueSelected = DataInternal[curKey];
    }

    public override void AddOrModifyEntry()
    {
        var newIndex = IndexSelected;
        if (DataInternal.ContainsKey(KeySelected))
        {
            DataInternal[KeySelected] = ValueSelected;
        }
        else
        {
            DataInternal.Add(KeySelected, ValueSelected);
            newIndex = DataInternal.Count - 1;
        }
        RefreshDiplayList(newIndex);
    }

    public override void RemoveEntry()
    {
        if (DataInternal.Count == 0 || IndexSelected == 0)
        {
            return;
        }
        IndexSelected = Math.Min(IndexSelected, DataInternal.Count - 1);
        DataInternal.Remove(DataInternal.Keys.ToArray()[IndexSelected]);
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