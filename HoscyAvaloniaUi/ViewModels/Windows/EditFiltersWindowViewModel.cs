using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditFiltersWindowViewModelBase : EditComplexListWindowViewModelBase<FilterModel>
{
    [ObservableProperty]
    public partial string SelectedName { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SelectedFilterText { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool SelectedEnabled { get; set; } = false;
    [ObservableProperty]
    public partial bool SelectedIgnoreCase { get; set; } = false;
    [ObservableProperty]
    public partial bool SelectedUseRegex { get; set; } = false;
}

[LoadIntoDiContainer(typeof(EditFiltersWindowViewModelBase), Lifetime.Transient)]
public class EditFiltersWindowViewModelImpl(ILogger logger) : EditFiltersWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditFiltersWindowViewModelBase>();

    protected override FilterModel CreateModelInternal(FilterModel? model)
    {
        var newModel = new FilterModel()
        {
            Enabled = SelectedEnabled,
            FilterString = SelectedFilterText,
            IgnoreCase = SelectedIgnoreCase,
            UseRegex = SelectedUseRegex
        };

        if (!string.IsNullOrWhiteSpace(SelectedName))
        {
            newModel.Name = SelectedName;
        }

        return newModel;
    }

    protected override string GetItemDisplayText(FilterModel item)
    {
        return item.ToString();
    }

    protected override string GetModelIdentifier(FilterModel selectedModel)
    {
        return selectedModel.Name;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedName;
    }

    protected override void LogModelAdded(FilterModel model)
    {
        _logger.Debug("Creating new Filter entry {entry}", model.ToString());
    }

    protected override void LogModelModified(FilterModel oldModel, FilterModel newModel)
    {
        _logger.Debug("Updating Filter entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(FilterModel model)
    {
        _logger.Debug("Removing Filter entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var model = new FilterModel();
        SetSelectedDataWithItem(model);
    }

    protected override void SetSelectedDataWithItem(FilterModel item)
    {
        SelectedEnabled = item.Enabled;
        SelectedFilterText = item.FilterString;
        SelectedIgnoreCase = item.IgnoreCase;
        SelectedName = item.Name;
        SelectedUseRegex = item.UseRegex;
    }
}

#if DEBUG
public class EditFiltersWindowViewModelPreview : EditFiltersWindowViewModelBase
{
    protected override FilterModel CreateModelInternal(FilterModel? model)
    {
        return new();
    }
}
#endif