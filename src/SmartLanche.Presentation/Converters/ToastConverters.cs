using System;
using System.Globalization;
using System.Windows.Data;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.Converters;

public class ToastIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ToastType type ? type switch
        {
            ToastType.Success => "✓",
            ToastType.Error => "✕",
            ToastType.Warning => "⚠",
            ToastType.Info => "ℹ",
            _ => ""
        } : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ToastColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ToastType type ? type switch
        {
            ToastType.Success => "#166534",
            ToastType.Error => "#991B1B",
            ToastType.Warning => "#92400E",
            ToastType.Info => "#1E40AF",
            _ => "#374151"
        } : "#374151";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}