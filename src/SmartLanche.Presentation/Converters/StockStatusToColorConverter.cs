using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class StockStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StockStatus status)
        {
            return status switch
            {
                StockStatus.Normal => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),      // Green #16A34A
                StockStatus.Low => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),         // Amber #F59E0B
                StockStatus.OutOfStock => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),   // Red #DC2626
                _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))                        // Gray #64748B
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}