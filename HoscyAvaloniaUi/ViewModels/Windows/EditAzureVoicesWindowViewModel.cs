using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditAzureVoicesWindowViewModelBase : EditComplexListWindowViewModelBase<AzureTtsVoiceModel>
{
    [ObservableProperty]
    public partial string SelectedName { get; set; }
    [ObservableProperty]
    public partial string SelectedVoice { get; set; }
    [ObservableProperty]
    public partial string SelectedLanguage { get; set; }
}

[LoadIntoDiContainer(typeof(EditAzureVoicesWindowViewModelBase), Lifetime.Transient)]
public class EditAzureVoicesWindowViewModelImpl
(
    ILogger logger
) 
: EditAzureVoicesWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditAzureVoicesWindowViewModelImpl>();

    protected override AzureTtsVoiceModel CreateModelInternal(AzureTtsVoiceModel? model)
    {
        var newModel = new AzureTtsVoiceModel();

        if (!string.IsNullOrWhiteSpace(SelectedName))
        {
            newModel.Name = SelectedName;
        }
        if (!string.IsNullOrWhiteSpace(SelectedLanguage))
        {
            newModel.Language = SelectedLanguage;
        }
        if (!string.IsNullOrWhiteSpace(SelectedVoice))
        {
            newModel.Voice = SelectedVoice;
        }

        return newModel;
    }

    protected override string GetItemDisplayText(AzureTtsVoiceModel item)
    {
        return item.ToString();
    }

    protected override string GetModelIdentifier(AzureTtsVoiceModel selectedModel)
    {
        return selectedModel.Name;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedName;
    }

    protected override void LogModelAdded(AzureTtsVoiceModel model)
    {
        _logger.Debug("Creating new AzureTtsVoice entry {entry}", model.ToString());
    }

    protected override void LogModelModified(AzureTtsVoiceModel oldModel, AzureTtsVoiceModel newModel)
    {
        _logger.Debug("Updating AzureTtsVoice entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(AzureTtsVoiceModel model)
    {
        _logger.Debug("Removing AzureTtsVoice entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var item = new AzureTtsVoiceModel();
        SetSelectedDataWithItem(item);
    }

    protected override void SetSelectedDataWithItem(AzureTtsVoiceModel item)
    {
        SelectedLanguage = item.Language;
        SelectedVoice = item.Voice;
        SelectedName = item.Name;
    }

}

#if DEBUG
public class EditAzureVoicesWindowViewModelPreview : EditAzureVoicesWindowViewModelBase
{
    protected override AzureTtsVoiceModel CreateModelInternal(AzureTtsVoiceModel? selectedModel)
    {
        return new();
    }
}
#endif