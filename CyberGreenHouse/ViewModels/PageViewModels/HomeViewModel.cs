using Avalonia.Threading;
using CyberGreenHouse.Models;
using CyberGreenHouse.Models.Response;
using CyberGreenHouse.Tools;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CyberGreenHouse.ViewModels.PageViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private Sensors? _sensorData;
        private DispatcherTimer? _timer;
        private WaterValue _waterValueData;
        private bool _isEnableButton = true;

        public Sensors? SensorData
        {
            get => _sensorData;
            set => this.RaiseAndSetIfChanged(ref _sensorData, value);
        }

        public WaterValue WaterValueData
        {
            get => _waterValueData;
            set => this.RaiseAndSetIfChanged(ref _waterValueData, value);
        }

        public bool IsEnableButton
        {
            get => _isEnableButton;
            set => this.RaiseAndSetIfChanged(ref _isEnableButton, value);
        }

        public ReactiveCommand<WaterValue, Unit> SwitchWaterValueCommand { get; }



        public HomeViewModel()
        {
            LoadData(); //PreLoad

            SwitchWaterValueCommand = ReactiveCommand.CreateFromTask<WaterValue>(SwitchWaterValue);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += async (sender, e) =>
            {
                await LoadData();
            };
            _timer.Start();
        }

        private async Task SwitchWaterValue(WaterValue arg)
        {
            IsEnableButton = false;
            string state = (string)DataConverter.ConvertBack<WaterValue>(arg);
            await DataService.SetWaterValue(state);
            await RunAfterDelay(15000, () =>
            {
                IsEnableButton = true;
            });
        }

        private async Task LoadData()
        {
            await LoadSensorDataAsync();
            await LoadValue();
        }

        private async Task LoadSensorDataAsync()
        {
            var result = await DataService.GetSensorData();

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
                SensorData = DataConverter.Convert<Sensors>(result.Data);
            }
        }

        private async Task LoadValue()
        {
            var result = await DataService.GetValueState();

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
                WaterValueData = DataConverter.Convert<WaterValue>(result.Data);
            }
        }

        /// <summary>
        /// Выполняет действие с задержкой (без async/await).
        /// </summary>
        /// <param name="delayMs">Задержка в миллисекундах.</param>
        /// <param name="action">Действие для выполнения.</param>
        private static async Task RunAfterDelay(int delayMs, Action action)
        {
            await Task.Delay(delayMs);
            action();
        }
    }
}
