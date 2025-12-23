using Avalonia.Controls;

namespace Hoscy.ViewModels.Core;

public partial class MainWindowViewModel : ViewModelBase
{
    private UserControl _currentView = null!;
    public UserControl CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
}
