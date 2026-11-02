using System;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class MovementOriginToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MovementOrigin origin)
        {
            var field = origin.GetType().GetField(origin.ToString());
            var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return attr?.Description ?? origin.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}