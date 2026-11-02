using System.Globalization;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public sealed class StatusPagamentoToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            StatusPagamentoPendente.NaoPago => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)),
            StatusPagamentoPendente.Pago => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)),
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}