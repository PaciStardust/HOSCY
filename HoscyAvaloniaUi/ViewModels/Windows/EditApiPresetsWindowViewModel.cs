using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditApiPresetsWindowViewModelBase : EditComplexListWindowViewModelBase<ApiPresetModel>
{
    [ObservableProperty]
    public partial string SelectedPresetName { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SelectedTargetUrl { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SelectedResultField { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int SelectedTimeoutMs { get; set; } = 0;
    [ObservableProperty]
    public partial string SelectedContentType { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SelectedContentToSend { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string SelectedAuthHeader { get; set; } = string.Empty;

    public virtual void EditHeaders(Window window) { }
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

    protected override string GetItemDisplayText(ApiPresetModel item)
    {
        return item.Name;
    }

    protected override string GetModelIdentifier(ApiPresetModel model)
    {
        return base.GetModelIdentifier(model);
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedPresetName;
    }

    protected override void LogModelAdded(ApiPresetModel model)
    {
        _logger.Debug("Creating new API Preset entry {entry}", model.ToString());
    }

    protected override void LogModelModified(ApiPresetModel oldModel, ApiPresetModel newModel)
    {
        _logger.Debug("Updating API Preset entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(ApiPresetModel model)
    {
        _logger.Debug("Removing API Preset entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var sample = new ApiPresetModel();
        SelectedPresetName = sample.Name;
        SelectedTargetUrl = sample.TargetUrl;
        SelectedResultField = sample.ResultField;
        SelectedTimeoutMs = sample.ConnectionTimeout;
        SelectedContentType = sample.ContentType;
        SelectedContentToSend = sample.SentData;
        SelectedAuthHeader = sample.Authorization;
    }

    protected override void SetSelectedDataWithItem(ApiPresetModel item)
    {
        SelectedPresetName = item.Name;
        SelectedTargetUrl = item.TargetUrl;
        SelectedResultField = item.ResultField;
        SelectedTimeoutMs = item.ConnectionTimeout;
        SelectedContentType = item.ContentType;
        SelectedContentToSend = item.SentData;
        SelectedAuthHeader = item.Authorization;
    }

    public override void EditHeaders(Window window)
    {
        var selectedModel = GetSelectedModel();
        if (selectedModel is null)
        {
            return;
        }
        _popupFactory.OpenEditDict($"Editing headers for API preset {selectedModel.Name}", "Header Name", "Header Value", selectedModel.HeaderValues, window);
    }

    protected override ApiPresetModel CreateModelInternal(ApiPresetModel? selectedModel)
    {
        var model = new ApiPresetModel();

        if (!string.IsNullOrWhiteSpace(SelectedPresetName))
            model.Name = SelectedPresetName;

        if (!string.IsNullOrWhiteSpace(SelectedTargetUrl))
            model.TargetUrl = SelectedTargetUrl;

        if (!string.IsNullOrWhiteSpace(SelectedResultField))
            model.ResultField = SelectedResultField;

        model.ConnectionTimeout = SelectedTimeoutMs;

        if (!string.IsNullOrWhiteSpace(SelectedContentToSend))
            model.SentData = SelectedContentToSend;

        if (!string.IsNullOrWhiteSpace(SelectedContentType))
            model.ContentType = SelectedContentType;

        if (selectedModel is not null)
            model.HeaderValues = selectedModel.HeaderValues
                .ToDictionary(x => x.Key, x => x.Value);

        if (!string.IsNullOrWhiteSpace(SelectedAuthHeader))
            model.Authorization = SelectedAuthHeader;

        return model;
    }
}

#if DEBUG
public class EditApiPresetsWindowViewModelPreview : EditApiPresetsWindowViewModelBase
{
    protected override ApiPresetModel CreateModelInternal(ApiPresetModel? selectedModel)
    {
        return new();
    }
}
#endif