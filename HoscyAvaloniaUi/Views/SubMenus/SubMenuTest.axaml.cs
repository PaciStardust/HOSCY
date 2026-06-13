using Avalonia.Controls;
using HoscyCore.Services.Dependency;

namespace HoscyAvaloniaUi.Views.SubMenus;

[PrototypeLoadIntoDiContainer(typeof(SubMenuTest), Lifetime.Transient)]
public partial class SubMenuTest : UserControl
{
    public SubMenuTest()
    {
        InitializeComponent();
    }
}