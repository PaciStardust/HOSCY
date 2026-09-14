using CommunityToolkit.Mvvm.ComponentModel;

namespace HoscyAvaloniaUi.ViewModels.Core;

public class ViewModelBase : ObservableObject
{
}

public class ViewModelBaseWithLoadedIndicator : ViewModelBase
{
    public bool Loaded { get; set; } = false;
}
