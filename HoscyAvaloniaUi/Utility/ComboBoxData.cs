using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public partial class ComboBoxData : ObservableObject
{
    public const string COMBO_BOX_NO_OPTIONS = "(No options available)";

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string[] Options { get; private set; }

    private readonly ILogger? _logger;
    private readonly string _id;

    public ComboBoxData(string[] options, string selected, ILogger? logger, string id)
    {
        _logger = logger;
        _id = id;
        (Options, Index) = RefreshItemsInternal(options, selected, _logger, _id);
    }
    public ComboBoxData() : this([], string.Empty, null, string.Empty) { }

    public string? GetSelected()
    {
        Index = Index.MinMax(-1, Options.Length - 1);
        if (Index == -1 || Options[Index] == COMBO_BOX_NO_OPTIONS)
        {
            return null;
        }
        
        var option = Options[Index];
        _logger?.Verbose("Value set to {value} for combo box {id}", option, _id);
        return option;
    }

    public void RefreshItems(string[] options, string selected)
    {
        (Options, Index) = RefreshItemsInternal(options, selected, _logger, _id);
    }

    private static (string[] Options, int Index) RefreshItemsInternal(string[] options, string selected, ILogger? logger, string id)
    {
        if (options.Length == 0)
        {
            return ([COMBO_BOX_NO_OPTIONS], 0);
        }

        logger?.Debug("Loading combo box {id} with value {value}", id, selected);
        var idx = options.IndexOf(selected);
        if (idx == -1)
        {
            logger?.Warning("Failed to find value {value} in combo box {id}", selected, id);
            idx = 0;
        }
        return (options, idx);
    }
}