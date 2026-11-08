using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SmartLanche.Presentation.Behaviors;

public class DateMaskTextBoxBehavior : Behavior<TextBox>
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private static readonly Regex DigitOnlyRegex = new(@"^[0-9\b]*$");
    private bool _isUpdating;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(DateTime?),
            typeof(DateMaskTextBoxBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public DateTime? Value
    {
        get => (DateTime?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MaskProperty =
        DependencyProperty.Register(
            nameof(Mask),
            typeof(string),
            typeof(DateMaskTextBoxBehavior),
            new PropertyMetadata("__/__/____"));

    public string Mask
    {
        get => (string)GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
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
        if (d is DateMaskTextBoxBehavior behavior && !behavior._isUpdating)
        {
            behavior.UpdateTextFromValue();
        }
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!DigitOnlyRegex.IsMatch(e.Text))
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

        // Show raw digits for easy editing
        if (Value.HasValue)
        {
            _isUpdating = true;
            AssociatedObject.Text = Value.Value.ToString("ddMMyyyy", PtBrCulture);
            _isUpdating = false;
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
        var digitsOnly = new string(textBox.Text.Where(char.IsDigit).ToArray());

        // Auto-format as user types: ddMMyyyy -> dd/MM/yyyy
        var formatted = FormatAsDateMask(digitsOnly);

        if (formatted != textBox.Text)
        {
            var caretIndex = textBox.CaretIndex;
            _isUpdating = true;
            textBox.Text = formatted;
            _isUpdating = false;

            // Adjust caret position
            textBox.CaretIndex = Math.Min(caretIndex, formatted.Length);
        }

        // Try parse complete date
        if (digitsOnly.Length == 8)
        {
            if (DateTime.TryParseExact(digitsOnly, "ddMMyyyy", PtBrCulture, DateTimeStyles.None, out var date))
            {
                _isUpdating = true;
                Value = date;
                _isUpdating = false;
            }
        }
        else if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            _isUpdating = true;
            Value = null;
            _isUpdating = false;
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdating || AssociatedObject is null)
            return;

        var digitsOnly = new string(AssociatedObject.Text.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 8 && DateTime.TryParseExact(digitsOnly, "ddMMyyyy", PtBrCulture, DateTimeStyles.None, out var date))
        {
            _isUpdating = true;
            Value = date;
            AssociatedObject.Text = date.ToString("dd/MM/yyyy", PtBrCulture);
            _isUpdating = false;
        }
        else if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            _isUpdating = true;
            Value = null;
            AssociatedObject.Text = string.Empty;
            _isUpdating = false;
        }
        else
        {
            // Invalid/incomplete date - clear
            _isUpdating = true;
            Value = null;
            AssociatedObject.Text = string.Empty;
            _isUpdating = false;
        }
    }

    private void UpdateTextFromValue()
    {
        if (AssociatedObject is null)
            return;

        _isUpdating = true;

        if (Value.HasValue)
        {
            AssociatedObject.Text = Value.Value.ToString("dd/MM/yyyy", PtBrCulture);
        }
        else
        {
            AssociatedObject.Text = string.Empty;
        }

        _isUpdating = false;
    }

    private string FormatAsDateMask(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        var result = string.Empty;
        for (int i = 0; i < digits.Length && i < 8; i++)
        {
            if (i == 2 || i == 4)
                result += "/";
            result += digits[i];
        }
        return result;
    }
}