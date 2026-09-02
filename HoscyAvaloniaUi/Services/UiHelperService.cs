using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using HoscyCore.Services.Core;

namespace HoscyAvaloniaUi.Services;

public class UiHelperService : IService
{
    public UiHelperService(Window parentWindow)
    {
        _parent = parentWindow;
        
        ValidBrush = UnknownBrush;
        InvalidBrush = UnknownBrush;
    }

    private readonly Window _parent;
    public Task ShowDialog(Window window)
    {
        if (_parent.IsActive && _parent.DataContext is not null)
        {
            return window.ShowDialog(_parent);
        }
        return Task.CompletedTask;
    }

    public void UpdateBrushes()
    {
        ValidBrush = (_parent.TryFindResource("ValidBrush", null, out var brush) 
            ? brush as IBrush : null) ?? UnknownBrush;
        InvalidBrush = (_parent.TryFindResource("InvalidBrush", null, out var brush2) 
            ? brush2 as IBrush : null) ?? UnknownBrush;
    }

    public IBrush UnknownBrush { get; init; } = new SolidColorBrush(Colors.HotPink);
    public IBrush ValidBrush { get; private set; }
    public IBrush InvalidBrush { get; private set; }
}