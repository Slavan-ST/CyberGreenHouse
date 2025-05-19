using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Models
{
    public class Schedule : ReactiveObject
    {
        private string _someTextProperty;
        private TimeSpan? _startTime;
        private TimeSpan? _endTime;
        private bool _isActive;
        private static int _count = 0;

        public string SomeTextProperty
        {
            get => _someTextProperty;
            set => this.RaiseAndSetIfChanged(ref _someTextProperty, value);
        }

        public TimeSpan? StartTime
        {
            get => _startTime;
            set => this.RaiseAndSetIfChanged(ref _startTime, value);
        }

        public TimeSpan? EndTime
        {
            get => _endTime;
            set => this.RaiseAndSetIfChanged(ref _endTime, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        public Schedule()
        {
            _count++;
            SomeTextProperty = $"Расписание #" + _count;
        }
    }
}
