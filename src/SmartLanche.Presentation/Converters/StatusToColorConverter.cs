using System;
using System.Globalization;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Aberto => "#6B7280",
                OrderStatus.EmPreparacao => "#2563EB",
                OrderStatus.Pronto => "#F59E0B",
                OrderStatus.Finalizado => "#16A34A",
                OrderStatus.Cancelado => "#6B7280",
                _ => "#6B7280"
            };
        }
        return "#6B7280";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}