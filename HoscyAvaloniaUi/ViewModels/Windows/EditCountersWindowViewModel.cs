using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public abstract partial class EditCountersWindowViewModelBase : EditComplexListWindowViewModelBase<CounterModel>
{
    [ObservableProperty]
    public partial bool SelectedEnabled { get; set; }
    [ObservableProperty]
    public partial bool SelectedDoDisplay { get; set; }
    [ObservableProperty]
    public partial string SelectedName { get; set; }
    [ObservableProperty]
    public partial string SelectedParameter { get; set; }
    [ObservableProperty]
    public partial uint SelectedCount { get; set; }
    [ObservableProperty]
    public partial float SelectedCooldown { get; set; }
    [ObservableProperty]
    public partial string SelectedLastUsed { get; set; }
}

[LoadIntoDiContainer(typeof(EditCountersWindowViewModelBase), Lifetime.Transient)]
public class EditCountersWindowViewModelImpl(ILogger logger) : EditCountersWindowViewModelBase
{
    private readonly ILogger _logger = logger.ForContext<EditCountersWindowViewModelBase>();

    protected override string GetItemDisplayText(CounterModel item)
    {
        return item.ToString();
    }

    protected override string GetModelIdentifier(CounterModel selectedModel)
    {
        return selectedModel.Name;
    }

    protected override string GetSelectedModelIdentifier()
    {
        return SelectedName;
    }

    protected override void LogModelAdded(CounterModel model)
    {
        _logger.Debug("Creating new Counter entry {entry}", model.ToString());
    }

    protected override void LogModelModified(CounterModel oldModel, CounterModel newModel)
    {
        _logger.Debug("Updating Counter entry {entryOld} => {newEntry}", oldModel.ToString(), newModel.ToString());
    }

    protected override void LogModelRemoved(CounterModel model)
    {
        _logger.Debug("Removing Counter entry {entry}", model.ToString());
    }

    protected override void SetSelectedDataNoItem()
    {
        var sample = new CounterModel();
        SelectedCooldown = sample.CooldownSeconds;
        SelectedCount = sample.Count;
        SelectedEnabled = sample.Enabled;
        SelectedName = sample.Name;
        SelectedParameter = sample.Parameter;
        SelectedDoDisplay = sample.DoDisplay;
        SelectedLastUsed = "Never";
    }

    protected override void SetSelectedDataWithItem(CounterModel item)
    {
        SelectedCooldown = item.CooldownSeconds;
        SelectedCount = item.Count;
        SelectedEnabled = item.Enabled;
        SelectedName =  item.Name;
        SelectedParameter = item.Parameter;
        SelectedDoDisplay = item.DoDisplay;
        SelectedLastUsed = item.LastUsed == DateTimeOffset.MinValue 
            ? "Never" : (DateTimeOffset.UtcNow - item.LastUsed).ToString() + " ago";
    }

    protected override CounterModel CreateModelInternal(CounterModel? model)
    {
        var newModel = new CounterModel()
        {
            CooldownSeconds = SelectedCooldown,
            Count = SelectedCount,
            DoDisplay = SelectedDoDisplay,
            Enabled = SelectedEnabled,
        };

        if (!string.IsNullOrWhiteSpace(SelectedName))
            newModel.Name = SelectedName;

        if (!string.IsNullOrWhiteSpace(SelectedParameter))
            newModel.Parameter = SelectedParameter;

        return newModel;
    }
}

#if DEBUG
public class EditCountersWindowViewModelPreview : EditCountersWindowViewModelBase
{
    protected override CounterModel CreateModelInternal(CounterModel? model)
    {
        return new();
    }
}
#endif