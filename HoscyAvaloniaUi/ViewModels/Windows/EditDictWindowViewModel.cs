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
    public partial string KeyName { get; set; } = "Key";
    [ObservableProperty]
    public partial string KeyWatermark { get; set; } = "Key...";
    [ObservableProperty]
    public partial string ValueName { get; set; } = "Value";
    [ObservableProperty]
    public partial string ValueWatermark { get; set; } = "Value...";
    [ObservableProperty]
    public partial List<string> DisplayedValues { get; set; } = [];

    [ObservableProperty]
    public partial string CurrentKey { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string CurrentValue { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int SelectedIndex { get; set; } = 0;

    public Dictionary<string, string> InternalValues { get; set; } = [];

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
        KeyName = keyName;
        KeyWatermark = keyName + "...";
        ValueName = valueName;
        ValueWatermark = valueName + "....";
        InternalValues = dict;
        RefreshDiplayList(0);
    }

    protected override void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in InternalValues) {
            newValues.Add($"{x.Key} : {x.Value}");
        }
        DisplayedValues = newValues;
        SelectedIndex = Math.Min(index, InternalValues.Count - 1);
    }

    public override void SelectionChanged()
    {
        if (SelectedIndex == -1)
        {
            CurrentKey = string.Empty;
            CurrentValue = string.Empty;
            return;
        }

        SelectedIndex = Math.Min(SelectedIndex, InternalValues.Count - 1);
        var curKey = InternalValues.Keys.ToArray()[SelectedIndex];
        CurrentKey = curKey;
        CurrentValue = InternalValues[curKey];
    }

    public override void AddOrModifyEntry()
    {
        var newIndex = SelectedIndex;
        if (InternalValues.ContainsKey(CurrentKey))
        {
            InternalValues[CurrentKey] = CurrentValue;
        }
        else
        {
            InternalValues.Add(CurrentKey, CurrentValue);
            newIndex = InternalValues.Count - 1;
        }
        RefreshDiplayList(newIndex);
    }

    public override void RemoveEntry()
    {
        if (InternalValues.Count == 0 || SelectedIndex == 0)
        {
            return;
        }
        SelectedIndex = Math.Min(SelectedIndex, InternalValues.Count - 1);
        InternalValues.Remove(InternalValues.Keys.ToArray()[SelectedIndex]);
        RefreshDiplayList(SelectedIndex - 1);
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