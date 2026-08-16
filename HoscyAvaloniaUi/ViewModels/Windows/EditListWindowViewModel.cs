using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditListWindowViewModelBase : EditComplexListWindowViewModelBase<string>
{
    [ObservableProperty]
    public partial string Title { get; protected set; }
    [ObservableProperty]
    public partial string ValueName { get; protected set; }
    [ObservableProperty]
    public partial string ValuePlaceholder { get; protected set; }

    [ObservableProperty]
    public partial string SelectedValue { get; set; }

    public virtual void InitExtra(List<string> values, string title, string valueName) { }
}

[LoadIntoDiContainer(typeof(EditListWindowViewModelBase), Lifetime.Transient)]
public partial class EditListWindowViewModelImpl(ILogger logger) : EditListWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditListWindowViewModelImpl>();
    private string _newValueText = "New Value";

    public override void InitExtra(List<string> values, string title, string valueName)
    {
        Title = title;
        ValueName = valueName;
        ValuePlaceholder = (valueName + " ...").Trim();
        Init(values);
    }

    protected override string CreateModelInternal(string? model)
    {
        return _newValueText;
    }

    protected override string GetItemDisplayText(string item)
    {
        return item;
    }

    protected override string GetModelIdentifier(string selectedModel)
    {
        return selectedModel;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedValue;
    }

    protected override void LogModelAdded(string model)
    {
        _logger.Debug("Creating new {type} entry {entry}", ValueName, model);
    }

    protected override void LogModelModified(string oldModel, string newModel)
    {
        _logger.Debug("Modifying {type} entry {entryOld} => {entryNew}", ValueName, oldModel, newModel);
    }

    protected override void LogModelRemoved(string model)
    {
        _logger.Debug("Removing {type} entry {entry}", ValueName, model);
    }

    protected override void SetSelectedDataNoItem()
    {
        SelectedValue = _newValueText;
    }

    protected override void SetSelectedDataWithItem(string item)
    {
        SelectedValue = item;
    }
}

#if DEBUG
public partial class EditListWindowViewModelPreview : EditListWindowViewModelBase
{
    public EditListWindowViewModelPreview()
    {
        ValueName = "Test";
        ValuePlaceholder = "Test ...";
    }

    protected override string CreateModelInternal(string? model)
    {
        return string.Empty;
    }
}
#endif