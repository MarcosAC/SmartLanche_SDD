using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class MovementTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MovementType type)
        {
            return type == MovementType.Entrada
                ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))  // Green
                : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));  // Red
        }
        return new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));  // Gray fallback
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}