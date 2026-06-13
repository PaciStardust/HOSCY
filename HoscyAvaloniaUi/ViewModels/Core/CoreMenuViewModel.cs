using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyAvaloniaUi.ViewModels.Core;

public partial class CoreMenuViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial UserControl CurrentSubmenu { get; set; }
}
