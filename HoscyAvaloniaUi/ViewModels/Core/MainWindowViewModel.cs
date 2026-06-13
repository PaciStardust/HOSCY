using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyAvaloniaUi.ViewModels.Core;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] 
    public partial UserControl CurrentView { get; set; }
}
