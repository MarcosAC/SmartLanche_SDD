using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;

        if (parameter is string param && param.Equals("reverse", StringComparison.OrdinalIgnoreCase))
        {
            isNull = !isNull;
        }

        return isNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}