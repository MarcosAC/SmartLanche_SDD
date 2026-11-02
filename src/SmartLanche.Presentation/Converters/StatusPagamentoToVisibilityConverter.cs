using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Presentation.Converters;

public sealed class StatusPagamentoToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not StatusPagamentoPendente status)
        {
            return Visibility.Collapsed;
        }

        var targetStatus = parameter?.ToString();

        if (string.IsNullOrEmpty(targetStatus))
        {
            return Visibility.Collapsed;
        }

        return Enum.TryParse<StatusPagamentoPendente>(targetStatus, out var target)
            && status == target
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}