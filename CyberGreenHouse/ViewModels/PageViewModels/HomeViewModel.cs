using Avalonia.Threading;
using CyberGreenHouse.Models;
using CyberGreenHouse.Models.Response;
using CyberGreenHouse.Tools;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace CyberGreenHouse.ViewModels.PageViewModels
{
    public class HomeViewModel : ViewModelBase, IDisposable
    {
        private Sensors? _sensorData;
        private DispatcherTimer? _timer;
        private WaterValue _waterValueData;
        private bool _isEnableButton = true;
        private bool _disposed;

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
            LoadData();

            SwitchWaterValueCommand = ReactiveCommand.CreateFromTask<WaterValue>(SwitchWaterValue);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            await LoadData();
        }

        private async Task SwitchWaterValue(WaterValue arg)
        {
            IsEnableButton = false;
            string state = DataConverter.ConvertBack<WaterValue>(arg) as string ?? string.Empty;
            await DataService.SetWaterValue(state);
            await RunAfterDelay(15000, () =>
            {
                IsEnableButton = true;
            });
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadSensorDataAsync(), LoadValue());
        }

        private async Task LoadSensorDataAsync()
        {
            var result = await DataService.GetSensorData();
            if (await ShowErrorIfAny(result)) return;
            SensorData = DataConverter.Convert<Sensors>(result.Data);
        }

        private async Task LoadValue()
        {
            var result = await DataService.GetValueState();
            if (await ShowErrorIfAny(result)) return;
            WaterValueData = DataConverter.Convert<WaterValue>(result.Data);
        }

        private async Task<bool> ShowErrorIfAny(BaseResponse result)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                var errorBox = MessageBoxManager
                    .GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await errorBox.ShowAsync();
                return true;
            }
            return false;
        }

        private static async Task RunAfterDelay(int delayMs, Action action)
        {
            await Task.Delay(delayMs);
            action();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_timer is not null)
                {
                    _timer.Tick -= OnTimerTick;
                    _timer.Stop();
                    _timer = null;
                }
            }

            _disposed = true;
        }
    }
}