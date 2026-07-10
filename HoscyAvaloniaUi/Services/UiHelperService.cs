using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using HoscyCore.Services.Core;

namespace HoscyAvaloniaUi.Services;

public class UiHelperService(Window parentWindow) : IService
{
    private readonly Window _parent = parentWindow;
    public void ShowDialog(Window window)
    {
        if (_parent.IsActive && _parent.DataContext is not null)
        {
            window.ShowDialog(_parent);
        }
    }
}