using Avalonia.Controls;
using CyberGreenHouse.ViewModels.PageViewModels;
using System.Diagnostics;

namespace CyberGreenHouse.Views.Pages
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }
    }
}
