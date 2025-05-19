using Avalonia;
using Avalonia.Styling;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.ViewModels.PageViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        // Текущая выбранная тема
        private int _selectedThemeIndex;
        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                ApplyTheme();
                this.RaiseAndSetIfChanged(ref _selectedThemeIndex, value);
            }
        }

        // Команды
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public SettingsViewModel()
        {
            // Инициализация текущей темы
            InitializeTheme();

            // Инициализация команд
            SaveCommand = ReactiveCommand.Create(SaveSettings);
        }

        private void InitializeTheme()
        {
            if (Application.Current is null) return;

            // Определяем текущую тему приложения
            var currentTheme = Application.Current.RequestedThemeVariant;
            SelectedThemeIndex = currentTheme switch
            {
                { Key: not null } when currentTheme == ThemeVariant.Light => 1,
                { Key: not null } when currentTheme == ThemeVariant.Dark => 2,
                _ => 0 // По умолчанию или системная
            };
        }

        private void SaveSettings()
        {
            if (Application.Current is null) return;

            // Применяем выбранную тему
            var newTheme = SelectedThemeIndex switch
            {
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            Application.Current.RequestedThemeVariant = newTheme;

            // Здесь можно добавить сохранение других настроек

        }

        // Метод для применения темы (можно вызывать при изменении SelectedThemeIndex)
        public void ApplyTheme()
        {
            if (Application.Current is null) return;

            var theme = SelectedThemeIndex switch
            {
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            Application.Current.RequestedThemeVariant = theme;
        }
    }
}
