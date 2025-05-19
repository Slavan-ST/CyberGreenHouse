using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CyberGreenHouse.ViewModels;
using CyberGreenHouse.Views;

namespace CyberGreenHouse;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };

            // На десктопе меню всегда открыто
            vm.IsMenuOpen = false;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime mobile)
        {
            var vm = new MainViewModel();
            mobile.MainView = new MainView { DataContext = vm };

            // На мобильных меню по умолчанию закрыто
            vm.IsMenuOpen = false;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
