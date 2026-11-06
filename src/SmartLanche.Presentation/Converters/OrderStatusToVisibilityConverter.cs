using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public class OrderStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OrderStatus status)
        {
            return Visibility.Collapsed;
        }

        if (parameter is not string param)
        {
            return Visibility.Collapsed;
        }

        bool visible = param switch
        {
            "status_aberto" => status == OrderStatus.Aberto,
            "status_empreparacao" => status == OrderStatus.EmPreparacao,
            "status_pronto" => status == OrderStatus.Pronto,
            "status_finalizado" => status == OrderStatus.Finalizado,
            "status_cancelado" => status == OrderStatus.Cancelado,
            "status_cancelable" => status == OrderStatus.Aberto || status == OrderStatus.EmPreparacao || status == OrderStatus.Pronto,
            "status_actionable" => status == OrderStatus.EmPreparacao || status == OrderStatus.Pronto,
            "status_closable" => status == OrderStatus.EmPreparacao || status == OrderStatus.Pronto,
            _ => false
        };

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}