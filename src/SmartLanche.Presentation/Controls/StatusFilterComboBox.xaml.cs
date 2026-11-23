using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SmartLanche.Presentation.Controls;

public partial class StatusFilterComboBox : UserControl
{
    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(
            nameof(SelectedValue),
            typeof(object),
            typeof(StatusFilterComboBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedValueChanged));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IEnumerable<StatusFilterItem>),
            typeof(StatusFilterComboBox),
            new PropertyMetadata(null));

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public IEnumerable<StatusFilterItem>? Items
    {
        get => (IEnumerable<StatusFilterItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public StatusFilterComboBox()
    {
        InitializeComponent();
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFilterComboBox control && control.PART_ComboBox != null)
        {
            control.PART_ComboBox.SelectedValue = e.NewValue;
        }
    }
}