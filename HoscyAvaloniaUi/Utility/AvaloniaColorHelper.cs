using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace HoscyAvaloniaUi.Utility;

public static class AvaloniaColorHelper
{
    private static readonly Dictionary<string, IBrush?> _brushes = [];
    private static readonly IBrush _missingBrush = new SolidColorBrush(Colors.HotPink);
    public static IBrush GetBrush(Control? control, string name)
    {
        if (_brushes.TryGetValue(name, out var brush))
        {
            return brush ?? _missingBrush;
        }

        if (control is not null)
        {
            if (control.TryFindResource(name, control.ActualThemeVariant, out var locatedBrush)
                && locatedBrush is IBrush validBrush)
            {
                _brushes[name] = validBrush;
                return validBrush;
            } 
            else
            {
                _brushes[name] = null;
            }
        }
        
        return _missingBrush;
    }
}