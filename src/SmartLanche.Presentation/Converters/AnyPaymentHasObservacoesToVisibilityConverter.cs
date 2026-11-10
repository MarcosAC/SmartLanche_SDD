using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class AnyPaymentHasObservacoesToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var observacoesProp = item.GetType().GetProperty("Observacoes");
                if (observacoesProp != null)
                {
                    var obsValue = observacoesProp.GetValue(item) as string;
                    if (!string.IsNullOrWhiteSpace(obsValue))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}