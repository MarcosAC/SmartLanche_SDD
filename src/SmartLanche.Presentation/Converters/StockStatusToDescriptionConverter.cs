using System;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class StockStatusToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StockStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return attr?.Description ?? status.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}