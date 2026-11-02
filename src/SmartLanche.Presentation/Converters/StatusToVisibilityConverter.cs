using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class StatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OrderStatus status && parameter is string param)
        {
            if (int.TryParse(param, out int expectedStatus))
            {
                return status == (OrderStatus)expectedStatus ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}