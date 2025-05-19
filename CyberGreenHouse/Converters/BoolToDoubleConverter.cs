using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Converters
{
    public class BoolToDoubleConverter : IValueConverter
    {
        public double TrueValue { get; set; } = 1;
        public double FalseValue { get; set; } = 0;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && b ? TrueValue : FalseValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
