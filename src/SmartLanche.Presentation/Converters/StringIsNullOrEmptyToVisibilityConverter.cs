using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class StringIsNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = string.IsNullOrEmpty(value as string);

        if (parameter is string param && param.Equals("inverse", StringComparison.OrdinalIgnoreCase))
        {
            isNullOrEmpty = !isNullOrEmpty;
        }
        else if (parameter is string param2 && param2.Equals("reverse", StringComparison.OrdinalIgnoreCase))
        {
            isNullOrEmpty = !isNullOrEmpty;
        }

        return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}