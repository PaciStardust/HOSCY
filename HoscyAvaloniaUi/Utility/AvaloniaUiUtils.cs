using System;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public static class AvaloniaUiUtils
{
    public const string COMBO_BOX_NO_OPTIONS = "(No options available)";
    public static (string[], int) ComboBoxLoad(string[] options, string? selected, ILogger? logger, string id)
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
            return (options, 0);
        }
        return (options, idx);
    }

    public static (string?, int) ComboBoxIsValid(string[] options, int idx, ILogger? logger, string id)
    {
        var newIdx = idx.MinMax(-1, options.Length - 1);
        if (newIdx != -1 && options[newIdx] != COMBO_BOX_NO_OPTIONS)
        {
            return(null, 0);
        }
        var opt = options[newIdx];
        logger?.Verbose("Value set to {value} for combo box {id}", opt, id);
        return (opt, newIdx);
    }
}