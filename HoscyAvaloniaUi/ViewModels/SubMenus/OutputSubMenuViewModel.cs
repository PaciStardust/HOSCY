using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class OutputSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial bool ModuleReloadNeeded { get; set; }
    public virtual void ModuleReloadClicked() { }
    public virtual void ModuleToggled() { }

    [ObservableProperty]
    public partial ComboBoxData ModuleApiPresetMessage { get; set; }
    [ObservableProperty]
    public partial ComboBoxData ModuleApiPresetNotification { get; set; }
    [ObservableProperty]
    public partial ComboBoxData ModuleApiPresetClear { get; set; }
    [ObservableProperty]
    public partial ComboBoxData ModuleApiPresetProcessing { get; set; }
    [ObservableProperty]
    public partial ComboBoxData ModuleApiTranslationFormat { get; set; }
    public virtual void ModuleApiEditPresets() { }
    public virtual void ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox box) { }
    public virtual void ModuleApiTranslationFormatChanged() { }
}

[PrototypeLoadIntoDiContainer(typeof(OutputSubMenuViewModelBase), Lifetime.Transient)]
public class OutputSubMenuViewModelImpl : OutputSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly PopupWindowFactory _popup;
    private readonly IOutputManagerService _output;

    public OutputSubMenuViewModelImpl(ConfigModel config, PopupWindowFactory popup, ILogger logger, IOutputManagerService output)
    {
        Config = config;
        _logger = logger.ForContext<OutputSubMenuViewModelImpl>();
        _popup = popup;
        _output = output;

        UpdateModuleStatus();

        var presetNames = Config.Api_Presets.Select(x => x.Name).ToArray();
        ModuleApiPresetMessage = new(presetNames, Config.Output_Api_Preset_Message, _logger, "ModuleApiPresetMessage");
        ModuleApiPresetNotification = new(presetNames, Config.Output_Api_Preset_Notification, _logger, "ModuleApiPresetNotification");
        ModuleApiPresetClear = new(presetNames, Config.Output_Api_Preset_Clear, _logger, "ModuleApiPresetClear");
        ModuleApiPresetProcessing = new(presetNames, Config.Output_Api_Preset_Processing, _logger, "ModuleApiPresetProcessing");
        ModuleApiTranslationFormat = new(Enum.GetNames<OutputTranslationFormat>(), 
            Enum.GetName(Config.Output_Api_TranslationFormat) ?? string.Empty, _logger, "ModuleApiTranslationFormat");
    }

    private void UpdateModuleStatus()
    {
        ModuleReloadNeeded = _output.IsHandlerRefreshNeeded();
    }
    public override void ModuleReloadClicked()
    {
        _logger.Information("Manually reloading output modules");
        var res = _output.RefreshHandlers();
        if (!res.IsOk)
        {
            _popup.OpenNotification("Failed reloading output modules", res.Msg.Message, true, true);
        }
        UpdateModuleStatus();
    }
    public override void ModuleToggled()
    {
        UpdateModuleStatus();
    }

    public override void ModuleApiEditPresets()
    {
        _logger.Information("Editing api presets");
        _popup.OpenEditApiPresets(Config.Api_Presets, null, ModuleApiReloadPresetBoxes);
    }
    private void ModuleApiReloadPresetBoxes()
    {
        _logger.Debug("Reloading all API Preset ComboBoxes");
        var presetNames = Config.Api_Presets.Select(x => x.Name).ToArray();
        ModuleApiPresetMessage.RefreshItems(presetNames, Config.Output_Api_Preset_Message);
        ModuleApiPresetNotification.RefreshItems(presetNames, Config.Output_Api_Preset_Notification);
        ModuleApiPresetClear.RefreshItems(presetNames, Config.Output_Api_Preset_Clear);
        ModuleApiPresetProcessing.RefreshItems(presetNames, Config.Output_Api_Preset_Processing);
    }
    public override void ModuleApiPresetChanged(OutputSubMenuModuleApiComboBox box)
    {
        var selected = box switch
        {
            OutputSubMenuModuleApiComboBox.Message => ModuleApiPresetMessage.GetSelected(),
            OutputSubMenuModuleApiComboBox.Notification => ModuleApiPresetNotification.GetSelected(),
            OutputSubMenuModuleApiComboBox.Clear => ModuleApiPresetClear.GetSelected(),
            OutputSubMenuModuleApiComboBox.Processing => ModuleApiPresetProcessing.GetSelected(),
            _ => null
        };
        if (selected is null) return;

        var match = Config.Api_Presets.FirstOrDefault(x => x.Name == selected);
        if (match is null)
        {
            _logger.Warning("Failed to find API preset match for value {val}", selected);
            return;
        }

        switch(box)
        {
            case OutputSubMenuModuleApiComboBox.Message: 
                Config.Output_Api_Preset_Message = selected;
                break;
            case OutputSubMenuModuleApiComboBox.Notification:
                Config.Output_Api_Preset_Notification = selected;
                break;
            case OutputSubMenuModuleApiComboBox.Clear:
                Config.Output_Api_Preset_Clear = selected;
                break;
            case OutputSubMenuModuleApiComboBox.Processing:
                Config.Output_Api_Preset_Processing = selected;
                break;
        }
    }
    public override void ModuleApiTranslationFormatChanged()
    {
        var selected = ModuleApiTranslationFormat.GetSelected();
        if (selected is null) return;

        if (!Enum.TryParse<OutputTranslationFormat>(selected, out var parsed))
        {
            _logger.Warning("Failed to parse drop down value {selected} to OutputTranslationFormat", selected);
            return;
        }
        Config.Output_Api_TranslationFormat = parsed;
    }
}

#if DEBUG
public class OutputSubMenuViewModelPreview : OutputSubMenuViewModelBase
{
    public OutputSubMenuViewModelPreview()
    {
        Config = new()
        {
            Output_Api_Enabled = true,
            Output_Voice_Enabled = true,
            Output_VrcTxt_Enabled = true
        };

        ModuleReloadNeeded = true;

        ModuleApiPresetMessage = new();
        ModuleApiPresetNotification = new();
        ModuleApiPresetClear = new();
        ModuleApiPresetProcessing = new();
        ModuleApiTranslationFormat = new();
    }
}
#endif

public enum OutputSubMenuModuleApiComboBox
{
    Message,
    Notification,
    Clear,
    Processing
}