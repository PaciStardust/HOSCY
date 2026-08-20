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

        if (options.Length == 0)
        {
            Index = 0;
            Options = [COMBO_BOX_NO_OPTIONS];
            return;
        }

        _logger?.Debug("Loading combo box {id} with value {value}", _id, selected);
        Options = options;
        var idx = Options.IndexOf(selected);
        if (idx == -1)
        {
            _logger?.Warning("Failed to find value {value} in combo box {id}", selected, _id);
            Index = 0;
        }
        Index = idx;
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
}