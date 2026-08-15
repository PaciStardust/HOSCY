using System;
using System.Collections.Generic;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Utility;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditComplexListWindowViewModelBase<Tdata> : ViewModelBase where Tdata : class
{
    [ObservableProperty]
    public partial int SelectedIndex { get; set; } = 0;

    [ObservableProperty]
    public partial List<string> DataDisplayed { get; set; } = [];
    private List<Tdata> _dataInternal = [];

    public void Init(List<Tdata> data)
    {
        _dataInternal = data;
        RefreshDiplayList(0);
        SelectionChanged();
    }

    protected void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in _dataInternal) {
            newValues.Add(GetItemDisplayText(x));
        }
        DataDisplayed = newValues;
        SelectedIndex = index.MinMax(-1, _dataInternal.Count - 1);
    }
    protected virtual string GetItemDisplayText(Tdata item) { return item.ToString() ?? "Unnamed Item"; }

    public void SelectionChanged()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Count - 1);
        if (SelectedIndex == -1)
        {
            SetSelectedDataNoItem();
            return;
        }

        var selectedItem = _dataInternal[SelectedIndex];
        SetSelectedDataWithItem(selectedItem);
    }
    protected virtual void SetSelectedDataNoItem() { }
    protected virtual void SetSelectedDataWithItem(Tdata item) { }

    public void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(GetSelectedModelIdentifier()))
        {
            return;
        }
        var model = CreateModel();
        LogModelAdded(model);
        _dataInternal.Add(model);
        RefreshDiplayList(_dataInternal.Count - 1);
    }
    protected virtual void LogModelAdded(Tdata model) { }

    public void RemoveEntry()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Count - 1);
        if (SelectedIndex == -1)
        {
            return;
        }

        LogModelRemoved(_dataInternal[SelectedIndex]);
        _dataInternal.RemoveAt(SelectedIndex);
        RefreshDiplayList(SelectedIndex - 1);
    }
    protected virtual void LogModelRemoved(Tdata model) { }

    public void ModifyEntry()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Count - 1);
        if (SelectedIndex == -1)
        {
            AddEntry();
            return;
        }

        var model = CreateModel();
        LogModelModified(_dataInternal[SelectedIndex], model);
        _dataInternal[SelectedIndex] = model;
        RefreshDiplayList(SelectedIndex);
    }
    protected virtual void LogModelModified(Tdata oldModel, Tdata newMoldel) { }

    public void KeyPressed(KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;

        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Count - 1);
        if (SelectedIndex != -1 && GetSelectedModelIdentifier() == GetModelIdentifier(_dataInternal[SelectedIndex]))
        {
            ModifyEntry();
        }
        else
        {
            AddEntry();
        }
    }

    protected Tdata CreateModel()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, _dataInternal.Count - 1);
        return CreateModelInternal(SelectedIndex == -1 ? null : _dataInternal[SelectedIndex]);
    }
    protected abstract Tdata CreateModelInternal(Tdata? model);
    protected virtual string GetSelectedModelIdentifier() { return CreateModelInternal(null).ToString() ?? "Unknown Identifier"; }
    protected virtual string GetModelIdentifier(Tdata selectedModel) { return GetSelectedModelIdentifier(); }

    protected Tdata? GetSelectedModel()
    {
        SelectedIndex = Math.Min(SelectedIndex, _dataInternal.Count - 1);
        return SelectedIndex == -1 ? null : _dataInternal[SelectedIndex];
    }
}