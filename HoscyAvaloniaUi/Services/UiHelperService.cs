using System.Threading.Tasks;
using Avalonia.Controls;
using HoscyCore.Services.Core;

namespace HoscyAvaloniaUi.Services;

public class UiHelperService(Window parentWindow) : IService
{
    private readonly Window _parent = parentWindow;
    public Task ShowDialog(Window window)
    {
        if (_parent.IsActive && _parent.DataContext is not null)
        {
            return window.ShowDialog(_parent);
        }
        return Task.CompletedTask;
    }
}