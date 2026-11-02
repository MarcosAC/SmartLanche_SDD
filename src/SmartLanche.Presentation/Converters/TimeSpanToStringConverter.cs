using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}min";
            }
            if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes}min {timeSpan.Seconds}s";
            }
            return $"{timeSpan.Seconds}s";
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}