using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = false;

        if (value is bool b)
        {
            boolValue = b;
        }
        else if (value is int count)
        {
            boolValue = count > 0;
        }
        else if (value is long l)
        {
            boolValue = l > 0;
        }

        if (parameter is string param && param.Equals("inverse", StringComparison.OrdinalIgnoreCase))
        {
            boolValue = !boolValue;
        }
        else if (parameter is string param2 && param2.Equals("reverse", StringComparison.OrdinalIgnoreCase))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}