using CyberGreenHouse.Models;
using CyberGreenHouse.Tools;
using DynamicData.Binding;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using MsBox.Avalonia;

namespace CyberGreenHouse.ViewModels.PageViewModels
{
    public class ScheduleViewModel : ViewModelBase
    {
        private ObservableCollection<Schedule> _schedules = new ObservableCollection<Schedule>();
        private bool canUpdate = false;

        public ObservableCollection<Schedule> Schedules
        {
            get => _schedules;
            set => this.RaiseAndSetIfChanged(ref _schedules, value);
        }



        public ScheduleViewModel()
        {
            LoadData();

        }

        private async void LoadData()
        {
            var result = await DataService.GetSchedule();

            if (result.ErrorMessage != string.Empty)
            {
                if (result.ErrorMessage != string.Empty)
                {
                    var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await errorBox.ShowAsync();
                }
            }
            else
            {
                Schedules = DataConverter.Convert<ObservableCollection<Schedule>>(result.Data);

                foreach (var item in Schedules)
                {
                    SubscribeToItemChanges(item);
                }

                canUpdate = true;
            }
        }

        private void SubscribeToItemChanges(Schedule item)
        {
            item.WhenAnyValue(x => x.StartTime)
                .Subscribe(x => UpdateSchedulesOnServer());

            item.WhenAnyValue(x => x.EndTime)
                .Subscribe(x => UpdateSchedulesOnServer());

            item.WhenAnyValue(x => x.IsActive)
                .Subscribe(x => UpdateSchedulesOnServer());
        }

        private async void UpdateSchedulesOnServer()
        {
            if (canUpdate)
            {
                object data = DataConverter.ConvertBack<ObservableCollection<Schedule>>(Schedules);
                var result = await DataService.SetSchedules((string[])data);
                if (result.ErrorMessage != string.Empty)
                {
                    var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await errorBox.ShowAsync();
                }
            }
        }
    }
}
