using Avalonia.Controls;
using Avalonia.Input;
using CyberGreenHouse.ViewModels;

namespace CyberGreenHouse.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Rectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsMenuOpen = false;
        }
        e.Handled = true;
    }
}
