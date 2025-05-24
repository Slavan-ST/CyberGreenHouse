using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;
using CyberGreenHouse.ViewModels.PageViewModels;
using System.Diagnostics;
using System.Linq;

namespace CyberGreenHouse.Views.Pages
{
    public partial class ScheduleView : UserControl
    {
        public ScheduleView()
        {
            InitializeComponent();
            DataContext = new ScheduleViewModel();
        }
    }
}
