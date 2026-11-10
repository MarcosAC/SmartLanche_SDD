using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class AnyPaymentHasNotesToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var observacoesProp = item.GetType().GetProperty("Notes");
                if (observacoesProp != null)
                {
                    var obsValue = observacoesProp.GetValue(item) as string;
                    if (!string.IsNullOrWhiteSpace(obsValue))
                    {
                        return Visibility.Visible;
                    }
                }
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}