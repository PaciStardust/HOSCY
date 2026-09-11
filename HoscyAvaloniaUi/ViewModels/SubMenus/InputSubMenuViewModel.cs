using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Input;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class InputSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set;}

    [ObservableProperty]
    public partial string LastSent { get; set; }

    [ObservableProperty]
    public partial string TextboxContent { get; set; }

    [ObservableProperty]
    public partial List<string> Presets { get; protected set; }
    [ObservableProperty]
    public partial int PresetIndex { get; set; }
    public virtual void PresetSelected() { }

    public virtual void SendClicked() { }
    public virtual void ClearClicked() { }
    public virtual void PresetsClicked() { }
    public virtual void HistoryClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(InputSubMenuViewModelBase), Lifetime.Transient)]
public class InputSubMenuViewModelImpl : InputSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly IOutputManagerService _output;
    private readonly IInputService _input;
    private readonly OutputHistoryService _outputHistory;
    private readonly PopupWindowFactory _popup;

    public InputSubMenuViewModelImpl
    (
        ConfigModel config,
        ILogger logger,
        IOutputManagerService output,
        IInputService input,
        OutputHistoryService outputHistory,
        PopupWindowFactory popup
    )
    {
        Config = config;
        _logger = logger.ForContext<InputSubMenuViewModelImpl>();
        _output = output;
        _input = input;
        _outputHistory = outputHistory;
        _popup = popup;

        LastSent = _outputHistory.GetLastMessage()?.Message ?? "Good Morning!";
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        _logger.Debug("Refreshing text presets");
        Presets = [.. Config.ManualInput_TextPresets.Keys];
        PresetIndex = -1;
    }
    public override void PresetSelected()
    {
        PresetIndex = PresetIndex.MinMax(-1, Presets.Count - 1);
        if (PresetIndex == -1) return;

        var selected = Presets[PresetIndex];

        if (!Config.ManualInput_TextPresets.TryGetValue(selected, out var value))
        {
            _logger.Warning($"Failed to locate a preset value for the key \"{selected}\"");
            return;
        }

        TextboxContent = value;
        PresetIndex = -1;
    }

    public override void ClearClicked()
    {
        _output.Clear();
    }
    public override void PresetsClicked()
    {
        _logger.Information("Editing input presets");
        _popup.OpenEditDict("Editing Input Presets", "Preset Name", "Preset Text", Config.ManualInput_TextPresets, null);
    }
    public override void HistoryClicked()
    {
        var history = _outputHistory.GetLastMessages().Select(x => x.Message).ToArray();
        _popup.OpenDisplayList("Message History", "Message", history, null);
    }
    public override void SendClicked()
    {
        TextboxContent = TextboxContent?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(TextboxContent))
        {
            TextboxContent = LastSent;
        }
        else
        {
            _input.SendManualMessage(TextboxContent);
            LastSent = TextboxContent;
            TextboxContent = string.Empty;
        }
    }
}

#if DEBUG
public class InputSubMenuViewModelPreview : InputSubMenuViewModelBase
{
    public InputSubMenuViewModelPreview()
    {
        Presets = ["A", "B", "C"];
    }
}
#endif