using CyberGreenHouse.Tools;
using ReactiveUI;

namespace CyberGreenHouse.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private DataService? _dataService;

    protected DataService DataService
    {
        get
        {
            if (_dataService == null)
            {
                _dataService = new DataService();
            }
            return _dataService;
        }
    }
}
