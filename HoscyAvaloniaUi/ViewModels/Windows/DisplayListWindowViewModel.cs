using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyAvaloniaUi.ViewModels.Windows;

public partial class DisplayListWindowViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; protected set; }
    [ObservableProperty]
    public partial string ValueName { get; protected set; }
    [ObservableProperty]
    public partial string ValuePlaceholder { get; protected set; }
    [ObservableProperty]
    public partial string SelectedText { get; set; }
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }
    [ObservableProperty]
    public partial string[] DisplayedList { get; protected set; }

    public virtual void SelectionChanged() { }

    public void Init(string[] data, string title, string valueName)
    {
        DisplayedList = data;
        Title = title;
        ValueName = valueName;
        ValuePlaceholder = (valueName + " ...").Trim();
        SelectionChanged();
    }
}

[LoadIntoDiContainer(typeof(DisplayListWindowViewModelBase), Lifetime.Transient)]
public partial class DisplayListWindowViewModelImpl : DisplayListWindowViewModelBase
{
    public override void SelectionChanged()
    {
        SelectedIndex = SelectedIndex.MinMax(-1, DisplayedList.Length - 1);
        SelectedText = SelectedIndex == -1 ? "(No data available)" : DisplayedList[SelectedIndex];
    }
}

#if DEBUG
public partial class DisplayListWindowViewModelPreview : DisplayListWindowViewModelBase
{
    public DisplayListWindowViewModelPreview()
    {
        ValueName = "Value";
        ValuePlaceholder = "Value ...";
    }
}
#endif