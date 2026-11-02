using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field is not null)
            {
                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                if (attr is not null)
                {
                    var description = attr.Description;
                    // Support ConverterParameter for suffix
                    if (parameter is string param && param.Equals("Suffix", StringComparison.OrdinalIgnoreCase))
                    {
                        return enumValue switch
                        {
                            DiscountType.Percentage => "%",
                            DiscountType.FixedAmount => " R$",
                            _ => string.Empty
                        };
                    }
                    return description;
                }
            }
            return enumValue.ToString();
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}