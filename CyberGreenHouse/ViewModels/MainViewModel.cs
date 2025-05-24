using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using CyberGreenHouse.Models;
using CyberGreenHouse.Tools;
using CyberGreenHouse.Views;
using CyberGreenHouse.Views.Pages;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CyberGreenHouse.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        #region Privates
        private UserControl _currentView;
        private List<UserControl> _pages;
        private bool _isMenuOpen = true;
        private double _contentOpacity = 1.0;
        private string _title = "Главная";
        #endregion Privates

        #region Publics
        public UserControl CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => this.RaiseAndSetIfChanged(ref _isMenuOpen, value);
        }

        public double ContentOpacity
        {
            get => _contentOpacity;
            set => this.RaiseAndSetIfChanged(ref _contentOpacity, value);
        }

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public List<UserControl> Pages
        {
            get => _pages;
            set => this.RaiseAndSetIfChanged(ref _pages, value);
        }
        #endregion Publics

        public ReactiveCommand<Unit, Unit> ToggleMenuCommand { get; }
        public ReactiveCommand<string, Unit> NavigateCommand { get; }

        public MainViewModel()
        {
            // Инициализация команд
            ToggleMenuCommand = ReactiveCommand.Create(ToggleMenu);
            NavigateCommand = ReactiveCommand.CreateFromTask<string>(Navigate);
            Pages = new List<UserControl>();
            Pages.AddRange(
            [
                new HomeView(),
                new AboutView(),
                new CalibrationView(),
                new SettingsView(),
                new ScheduleView(),
            ]);

            CurrentView = Pages.FirstOrDefault(view => view is HomeView);
        }

        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private async Task Navigate(string viewName)
        {
            ContentOpacity = 0.2;
            await Task.Delay(500);
            switch (viewName)
            {
                case "HomeView":
                    CurrentView = Pages.FirstOrDefault(view => view is HomeView);
                    Title = "Главная";
                    break;
                case "ScheduleView":
                    CurrentView = Pages.FirstOrDefault(view => view is ScheduleView);
                    Title = "Расписание";
                    break;
                case "CalibrationView":
                    CurrentView = Pages.FirstOrDefault(view => view is CalibrationView);
                    Title = "Калибровка";
                    break;
                case "SettingsView":
                    CurrentView = Pages.FirstOrDefault(view => view is SettingsView);
                    Title = "Настройки";
                    break;
                case "AboutView":
                    CurrentView = Pages.FirstOrDefault(view => view is AboutView);
                    Title = "О программе";
                    break;
            }
            IsMenuOpen = false;
            ContentOpacity = 1.0;
        }
    }
}