using Avalonia.Controls;

namespace Hoscy.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    private UserControl _currentSubmenu = null!;
    public UserControl CurrentSubmenu
    {
        get => _currentSubmenu;
        set => SetProperty(ref _currentSubmenu, value);
    }
}
