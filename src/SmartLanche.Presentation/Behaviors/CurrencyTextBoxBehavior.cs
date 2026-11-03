using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SmartLanche.Presentation.Behaviors;

public class CurrencyTextBoxBehavior : Behavior<TextBox>
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private static readonly Regex ValidInputRegex = new(@"^[0-9,\b]*$");
    private bool _isUpdating;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(decimal?),
            typeof(CurrencyTextBoxBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty CultureProperty =
        DependencyProperty.Register(
            nameof(Culture),
            typeof(CultureInfo),
            typeof(CurrencyTextBoxBehavior),
            new PropertyMetadata(PtBrCulture));

    public CultureInfo Culture
    {
        get => (CultureInfo)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewTextInput += OnPreviewTextInput;
        AssociatedObject.TextChanged += OnTextChanged;
        AssociatedObject.LostFocus += OnLostFocus;
        AssociatedObject.GotFocus += OnGotFocus;
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

        UpdateTextFromValue();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
        AssociatedObject.TextChanged -= OnTextChanged;
        AssociatedObject.LostFocus -= OnLostFocus;
        AssociatedObject.GotFocus -= OnGotFocus;
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CurrencyTextBoxBehavior behavior && !behavior._isUpdating)
        {
            behavior.UpdateTextFromValue();
        }
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

        if (!ValidInputRegex.IsMatch(newText) || newText.Count(c => c == ',') > 1)
        {
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdating || AssociatedObject is null)
            return;

        // Show raw numeric value for easy editing
        if (Value.HasValue)
        {
            _isUpdating = true;
            AssociatedObject.Text = Value.Value.ToString("N2", Culture); // "1.234,56" without currency symbol
            _isUpdating = false;

            // Select all text for easy replacement
            AssociatedObject.SelectAll();
        }
        else
        {
            _isUpdating = true;
            AssociatedObject.Text = string.Empty;
            _isUpdating = false;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
            return;

        var textBox = (TextBox)sender;
        var text = textBox.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            Value = null;
            return;
        }

        if (decimal.TryParse(text, NumberStyles.Currency, Culture, out var value))
        {
            _isUpdating = true;
            Value = value;
            _isUpdating = false;
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        FormatTextAsCurrency();
    }

    private void UpdateTextFromValue()
    {
        if (AssociatedObject is null)
            return;

        _isUpdating = true;

        if (Value.HasValue)
        {
            AssociatedObject.Text = Value.Value.ToString("C2", Culture);
        }
        else
        {
            AssociatedObject.Text = string.Empty;
        }

        _isUpdating = false;
    }

    private void FormatTextAsCurrency()
    {
        if (_isUpdating || AssociatedObject is null)
            return;

        var text = AssociatedObject.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            Value = null;
            return;
        }

        if (decimal.TryParse(text, NumberStyles.Currency | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, Culture, out var value))
        {
            _isUpdating = true;
            Value = value;
            AssociatedObject.Text = value.ToString("C2", Culture);
            _isUpdating = false;
        }
        else if (decimal.TryParse(text.Replace("R$", "").Replace(" ", "").Trim(), NumberStyles.AllowDecimalPoint, Culture, out value))
        {
            _isUpdating = true;
            Value = value;
            AssociatedObject.Text = value.ToString("C2", Culture);
            _isUpdating = false;
        }
    }
}