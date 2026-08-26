using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditReplacementsWindowViewModelBase : EditComplexListWindowViewModelBase<ReplacementDataModel>
{
    [ObservableProperty]
    public partial string SelectedText { get; set; }
    [ObservableProperty]
    public partial string SelectedReplacement { get; set; }
    [ObservableProperty]
    public partial bool SelectedEnabled { get; set; }
    [ObservableProperty]
    public partial bool SelectedUseRegex { get; set; }
    [ObservableProperty]
    public partial bool SelectedIgnoreCase { get; set; }
}

[LoadIntoDiContainer(typeof(EditReplacementsWindowViewModelBase), Lifetime.Transient)]
public class EditReplacementsWindowViewModelImpl(ILogger logger) : EditReplacementsWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditReplacementsWindowViewModelImpl>();

    protected override ReplacementDataModel CreateModelInternal(ReplacementDataModel? model)
    {
        var newModel = new ReplacementDataModel()
        {
            Enabled = SelectedEnabled,
            IgnoreCase = SelectedIgnoreCase,
            UseRegex = SelectedUseRegex
        };

        if (!string.IsNullOrWhiteSpace(SelectedText))
        {
            newModel.Text = SelectedText;
        }
        if (!string.IsNullOrWhiteSpace(SelectedReplacement))
        {
            newModel.Replacement = SelectedReplacement;
        }

        return newModel;
    }

    protected override string GetItemDisplayText(ReplacementDataModel item)
    {
        return item.ToString();
    }

    protected override string GetModelIdentifier(ReplacementDataModel selectedModel)
    {
        return selectedModel.Text;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedText;
    }

    protected override void LogModelAdded(ReplacementDataModel model)
    {
        _logger.Debug("Creating new Replacement entry {entry}", model.ToString());
    }

    protected override void LogModelModified(ReplacementDataModel oldModel, ReplacementDataModel newModel)
    {
        _logger.Debug("Updating Replacement entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(ReplacementDataModel model)
    {
        _logger.Debug("Removing Replacement entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var model = new ReplacementDataModel();
        SetSelectedDataWithItem(model);
    }

    protected override void SetSelectedDataWithItem(ReplacementDataModel item)
    {
        SelectedEnabled = item.Enabled;
        SelectedIgnoreCase = item.IgnoreCase;
        SelectedReplacement = item.Replacement;
        SelectedText = item.Text;
        SelectedUseRegex = item.UseRegex;
    }
}

#if DEBUG
public class EditReplacementsWindowViewModelPreview : EditReplacementsWindowViewModelBase
{
    protected override ReplacementDataModel CreateModelInternal(ReplacementDataModel? model)
    {
        return new();
    }
}
#endif