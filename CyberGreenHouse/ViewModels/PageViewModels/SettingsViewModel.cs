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
        // Конструктор
        public SettingsViewModel()
        {
            InitializeTheme();
            SaveCommand = ReactiveCommand.Create(SaveSettings);
        }
      
        // Поля
        private int _selectedThemeIndex;

        // Свойства
        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedThemeIndex, value);
                ApplyTheme();
            }
        }

        // Команды
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        // Публичные методы
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

        // Приватные методы
        private void InitializeTheme()
        {
            if (Application.Current is null) return;

            var currentTheme = Application.Current.RequestedThemeVariant;
            SelectedThemeIndex = currentTheme switch
            {
                { Key: not null } when currentTheme == ThemeVariant.Light => 1,
                { Key: not null } when currentTheme == ThemeVariant.Dark => 2,
                _ => 0
            };
        }

        private void SaveSettings()
        {
            if (Application.Current is null) return;

            var newTheme = SelectedThemeIndex switch
            {
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            Application.Current.RequestedThemeVariant = newTheme;

            // Здесь можно добавить сохранение других настроек
        }
    }
}