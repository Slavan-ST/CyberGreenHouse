using Avalonia.Controls;
using CyberGreenHouse.Models;
using CyberGreenHouse.Tools;
using CyberGreenHouse.Views.Pages.Calibration;
using MsBox.Avalonia;
using MsBox.Avalonia.Models;
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
    public class CalibrationViewModel : ViewModelBase
    {
        private Plate _plate;
        private bool _isVisibleWizard = false;
        private int _currentStep = 0;
        private UserControl _currentStepView;
        private bool _isEnableButton = false;


        private string _confirmText = "Вы уверены, что хотите начать калибровку?\n\n" +
            "Это очень важный процесс занимающий около 15 минут, этой процедуре необходимо уделить должное внимания для коректно работы всего комплекса.\n" +
            "Калибровка это настройка датчиков влажности почвы, для того чтобы привести их показания к единому значению и более точных измерений.\n" +
            "Производит настройку стоит только в случаях замены датчиков или при высоких погрешнастях во влажности почвы.";

        public UserControl CurrentStepView
        {
            get => _currentStepView;
            set => this.RaiseAndSetIfChanged(ref _currentStepView, value);
        }

        public Plate Plate
        {
            get => _plate;
            set => this.RaiseAndSetIfChanged(ref _plate, value);
        }

        public bool IsVisibleWizard
        {
            get => _isVisibleWizard;
            set => this.RaiseAndSetIfChanged(ref _isVisibleWizard, value);
        }

        public int CurrentStep
        {
            get => _currentStep;
            set => this.RaiseAndSetIfChanged(ref _currentStep, value);
        }

        public bool IsEnableButton
        {
            get => _isEnableButton;
            set => this.RaiseAndSetIfChanged(ref _isEnableButton, value);
        }

        ReactiveCommand<Unit, Unit> StartCalibrationCommand { get; }
        ReactiveCommand<Unit, Unit> NextCommand { get; }

        public CalibrationViewModel()
        {
            StartCalibrationCommand = ReactiveCommand.Create(StartCalibration);
            NextCommand = ReactiveCommand.Create(NextStep);

            LoadData();
        }

        private async void StartCalibration()
        {
            Plate.State = 0;
            var confirmBox = MessageBoxManager.GetMessageBoxCustom(
                new MsBox.Avalonia.Dto.MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition
                        {
                            Name = "Да",
                            IsDefault = true,
                            IsCancel = false
                        },
                        new ButtonDefinition
                        {
                            Name = "Нет",
                            IsDefault = false,
                            IsCancel = true
                        }
                    },
                    ContentTitle = "Потверждение",
                    ContentMessage = _confirmText,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });

            if (await confirmBox.ShowAsync() == "Да")
            {
                IsEnableButton = false;
                Plate.State++;
                var rawState = DataConverter.ConvertBack<Plate>(Plate);
                var result = await DataService.SetPlateState((string)rawState);
                if (result.ErrorMessage != string.Empty)
                {
                    var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    await errorBox.ShowAsync();
                }
                CurrentStepView = new DryStep();
                IsVisibleWizard = true;
                CurrentStep = 1;
                await RunAfterDelay(15000, () =>
                {
                    IsEnableButton = true;
                });
            }
        }

        private async void NextStep()
        {
            IsEnableButton = false;
            CurrentStep++;
            string rawState;
            switch (CurrentStep)
            {
                case 2:
                    Plate.State++;
                    CurrentStepView = new WetStep();
                    break;
                case 3:
                    Plate.State = 0;
                    CurrentStepView = new FinishStep();
                    break;
                case 4:
                    IsVisibleWizard = false;
                    LoadData();
                    break;
            }
            rawState = (string)DataConverter.ConvertBack<Plate>(Plate);
            var result = await DataService.SetPlateState(rawState);
            if (result.ErrorMessage != string.Empty)
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await errorBox.ShowAsync();
            }
            await RunAfterDelay(15000, () =>
            {
                IsEnableButton = true;
            });
        }

        private async void LoadData()
        {
            var result = await DataService.GetPlateStatus();

            if (result.ErrorMessage != string.Empty)
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", result.ErrorMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await errorBox.ShowAsync();
            }
            else
            {
                Plate = DataConverter.Convert<Plate>(result.Data);
            }
        }

        /// <summary>
        /// Выполняет действие с задержкой
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
