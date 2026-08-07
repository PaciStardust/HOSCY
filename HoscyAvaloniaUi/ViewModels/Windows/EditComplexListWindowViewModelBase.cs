using System;
using System.Collections.Generic;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditComplexListWindowViewModelBase<Tdata> : ViewModelBase where Tdata : class
{
    [ObservableProperty]
    public partial int IndexSelected { get; set; } = 0;

    [ObservableProperty]
    public partial List<string> DataDisplayed { get; set; } = [];
    protected List<Tdata> _dataInternal = [];

    public void Init(List<Tdata> data)
    {
        _dataInternal = data;
        RefreshDiplayList(0);
    }

    protected void RefreshDiplayList(int index)
    {
        List<string> newValues = [];
        foreach (var x in _dataInternal) {
            newValues.Add(GetItemName(x));
        }
        DataDisplayed = newValues;
        IndexSelected = Math.Min(index, _dataInternal.Count - 1);
    }
    protected virtual string GetItemName(Tdata item) { return item.ToString() ?? "Unnamed Item"; }

    public void SelectionChanged()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            SetSelectedDataNoItem();
            return;
        }

        var selectedItem = _dataInternal[IndexSelected];
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
        LogModelCreated(model);
        _dataInternal.Add(model);
        RefreshDiplayList(_dataInternal.Count - 1);
    }
    protected virtual void LogModelCreated(Tdata model) { }

    public void RemoveEntry()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            return;
        }

        LogModelRemoved(_dataInternal[IndexSelected]);
        _dataInternal.RemoveAt(IndexSelected);
        RefreshDiplayList(IndexSelected - 1);
    }
    protected virtual void LogModelRemoved(Tdata model) { }

    public void ModifyEntry()
    {
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected == -1)
        {
            AddEntry();
            return;
        }

        var model = CreateModel();
        LogModelModified(_dataInternal[IndexSelected], model);
        _dataInternal[IndexSelected] = model;
        RefreshDiplayList(IndexSelected);
    }
    protected virtual void LogModelModified(Tdata oldModel, Tdata newMoldel) { }

    public void KeyPressed(KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;

        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        if (IndexSelected != -1 && GetSelectedModelIdentifier() == GetModelIdentifier(_dataInternal[IndexSelected]))
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
        IndexSelected = Math.Min(IndexSelected, _dataInternal.Count - 1);
        return CreateModelInternal(IndexSelected);
    }
    protected abstract Tdata CreateModelInternal(int index);
    protected virtual string GetSelectedModelIdentifier() { return CreateModelInternal(-1).ToString() ?? "Unknown Identifier"; }
    protected virtual string GetModelIdentifier(Tdata model) { return GetSelectedModelIdentifier(); }
}