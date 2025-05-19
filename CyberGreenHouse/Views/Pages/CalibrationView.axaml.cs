using Avalonia.Controls;
using CyberGreenHouse.ViewModels.PageViewModels;

namespace CyberGreenHouse.Views.Pages
{
    public partial class CalibrationView : UserControl
    {
        public CalibrationView()
        {
            InitializeComponent();
            DataContext = new CalibrationViewModel();
        }
    }
}
