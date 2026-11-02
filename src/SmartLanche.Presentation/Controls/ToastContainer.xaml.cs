using System.Windows;
using System.Windows.Controls;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.Controls;

public partial class ToastContainer : UserControl
{
    public ToastContainer()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ToastsProperty =
        DependencyProperty.Register(nameof(Toasts), typeof(IReadOnlyList<ToastItem>), typeof(ToastContainer), new PropertyMetadata(null));

    public IReadOnlyList<ToastItem> Toasts
    {
        get => (IReadOnlyList<ToastItem>)GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(nameof(DismissCommand), typeof(System.Windows.Input.ICommand), typeof(ToastContainer), new PropertyMetadata(null));

    public System.Windows.Input.ICommand DismissCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }
}