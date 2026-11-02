using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLanche.Presentation.Converters;

public class BoolToComboConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Combo" : "Simples";
        }
        return "Simples";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}