using System;
using System.Windows;
using System.Windows.Data;

namespace Jib.WPF.Controls.Mvvm
{
    [ValueConversion(typeof(bool?), typeof(Visibility))]
    public class NullableBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool setValue = false;
            if (value == null)
                return Visibility.Collapsed;
            else if (bool.TryParse(value.ToString(), out setValue) && setValue)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}