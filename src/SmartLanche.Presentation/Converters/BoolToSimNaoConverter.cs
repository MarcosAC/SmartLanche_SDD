using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class BoolToSimNaoConverter : IValueConverter
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

        // Support for inverse/reverse parameter
        if (parameter is string param && (param.Equals("inverse", StringComparison.OrdinalIgnoreCase) || param.Equals("reverse", StringComparison.OrdinalIgnoreCase)))
        {
            boolValue = !boolValue;
        }

        return boolValue ? "Sim" : "Não";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}