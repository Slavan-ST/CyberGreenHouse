using Avalonia.Controls;
using CyberGreenHouse.ViewModels.PageViewModels;

namespace CyberGreenHouse.Views.Pages
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
