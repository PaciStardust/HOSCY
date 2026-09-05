using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class TransSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial ComboBoxData OptionsSelectedModule { get; set; }
    [ObservableProperty]
    public partial string OptionsSelectedModuleDescription { get; protected set; }
    [ObservableProperty]
    public partial IBrush OptionsSelectedModuleStartStopBrush { get; protected set; }
    [ObservableProperty]
    public partial string OptionsSelectedModuleStartStopText { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleRestartEnabled { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleRestartNeeded { get; protected set; }
    public virtual void OptionsSelectedModuleChanged() { }
    public virtual void OptionsSelectedModuleStartStopClicked() { }
    public virtual void OptionsSelectedModuleRefreshClicked() { }
    public virtual void OptionsSelectedModuleRestartClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(TransSubMenuViewModelBase), Lifetime.Transient)]
public class TransSubMenuViewModelImpl : TransSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly ITranslationManagerService _trans;
    private readonly ITranslationModuleStartInfo[] _transInfosOrdered;
    private readonly PopupWindowFactory _popup;
    private readonly UiHelperService _uiHelper;

    public TransSubMenuViewModelImpl
    (
        ConfigModel config, 
        ILogger logger, 
        ITranslationManagerService trans,
        PopupWindowFactory popup,
        UiHelperService uiHelper
    )
    {
        Config = config;
        _logger = logger.ForContext<TransSubMenuViewModelImpl>();
        _trans = trans;
        _popup = popup;
        _uiHelper = uiHelper;

        OptionsSelectedModuleUpdateButtons(_trans.GetCurrentModuleStatus());
        _trans.OnModuleStatusChanged += OptionsSelectedModuleOnStatusChanged;


        _transInfosOrdered = [.. _trans.GetModuleInfos().OrderByDescending(x => x.Priority)];
        OptionsSelectedModule = new([.. _transInfosOrdered.Select(x => x.Name)], Config.Translation_SelectedModuleName, _logger, "OptionsSelectedModule");
        OptionsSelectedModuleUpdateComboBox();
    }

        public override void OptionsSelectedModuleChanged()
    {
        OptionsSelectedModuleUpdateComboBox();
    }
    private void OptionsSelectedModuleUpdateComboBox()
    {
        var description =  "Description: ";
        var flags = TranslationModuleConfigFlags.None;

        var selected = OptionsSelectedModule.GetSelected();
        if (selected is null)
        {
            description += "No module is selected";
        }
        else
        {
            var match = _transInfosOrdered.FirstOrDefault(x => x.Name == selected);
            if (match is null)
            {
                description += "Selected module not found";
            }
            else
            {
                description += match.Description;
                flags = match.ConfigFlags;
            }
        }

        Config.Translation_SelectedModuleName = selected ?? string.Empty;

        OptionsSelectedModuleDescription = description;
    }
    private void OptionsSelectedModuleOnStatusChanged(ServiceStatus status)
    {
        OptionsSelectedModuleUpdateButtons(status);
    }
    public override void OptionsSelectedModuleStartStopClicked()
    {
        if (_trans.GetCurrentModuleStatus() == ServiceStatus.Stopped)
        {
            _logger.Information("Starting translation module");
            OptionsSelectedModuleStartStopText = "Starting";
            var res = _trans.StartModule();
            res.IfFail(x => _popup.OpenNotification("Failed to start translation module", x.Message, true, true));
        } 
        else
        {
            _logger.Information("Stopping translation module");
            OptionsSelectedModuleStartStopText = "Stopping";
            var res = _trans.StopModule();
            res.IfFail(x => _popup.OpenNotification("Failed to stop translation module", x.Message, true, true));
        }
    }
    public override void OptionsSelectedModuleRefreshClicked()
    {
        _logger.Information("Refreshing translation module");
        var res = _trans.RefreshModule();
        res.IfFail(x => _popup.OpenNotification("Failed to refresh translation module", x.Message, true, true));
    }
    public override void OptionsSelectedModuleRestartClicked()
    {
        if (_trans.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            _logger.Information("Restarting translation module");
            var res = _trans.StopModule();
            res = res.IsOk ? _trans.StartModule() : res;
            res.IfFail(x => _popup.OpenNotification("Failed to restart translation module", x.Message, true, true));
        }
    }
    private void OptionsSelectedModuleUpdateButtons(ServiceStatus status)
    {
        var running = status != ServiceStatus.Stopped;

        OptionsSelectedModuleStartStopText = running ? "Running" : "Stopped";
        OptionsSelectedModuleStartStopBrush = running ? _uiHelper.ValidBrush : _uiHelper.InvalidBrush;
        OptionsSelectedModuleRestartEnabled = running;
    }
}

#if DEBUG
public class TransSubMenuViewModelPreview : TransSubMenuViewModelBase
{
    public TransSubMenuViewModelPreview()
    {
        Config = new();

        OptionsSelectedModule = new();
        OptionsSelectedModuleStartStopText = "Stopped";
        OptionsSelectedModuleRestartNeeded = true;
        OptionsSelectedModuleDescription = "Description Placeholder 123";
    }
}
#endif