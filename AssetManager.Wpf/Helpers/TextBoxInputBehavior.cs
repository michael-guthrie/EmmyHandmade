using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace AssetManager.Helpers;

/// <summary>
/// Restricts a TextBox to numeric input. Carried over from the .NET Framework app with one
/// change: it derives from <c>Microsoft.Xaml.Behaviors.Behavior&lt;T&gt;</c> instead of
/// <c>System.Windows.Interactivity.Behavior&lt;T&gt;</c>, the latter being .NET Framework only.
/// The validation logic is untouched.
/// </summary>
public class TextBoxInputBehavior : Behavior<TextBox>
{
    private const NumberStyles ValidNumberStyles =
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;

    public TextBoxInputBehavior()
    {
        InputMode = TextBoxInputMode.None;
        JustPositiveDecimalInput = false;
    }

    public TextBoxInputMode InputMode { get; set; }

    public static readonly DependencyProperty JustPositiveDecimalInputProperty =
        DependencyProperty.Register(nameof(JustPositiveDecimalInput), typeof(bool),
            typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(false));

    public bool JustPositiveDecimalInput
    {
        get => (bool)GetValue(JustPositiveDecimalInputProperty);
        set => SetValue(JustPositiveDecimalInputProperty, value);
    }

    public bool BeepOnInvalid { get; set; }

    private static string DecimalSeparator => CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewTextInput += AssociatedObjectPreviewTextInput;
        AssociatedObject.PreviewKeyDown += AssociatedObjectPreviewKeyDown;
        DataObject.AddPastingHandler(AssociatedObject, Pasting);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewTextInput -= AssociatedObjectPreviewTextInput;
        AssociatedObject.PreviewKeyDown -= AssociatedObjectPreviewKeyDown;
        DataObject.RemovePastingHandler(AssociatedObject, Pasting);
    }

    private void Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string))!;
            if (!IsValidInput(GetText(pastedText)))
            {
                Beep();
                e.CancelCommand();
            }
        }
        else
        {
            Beep();
            e.CancelCommand();
        }
    }

    private void AssociatedObjectPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var decimalIndex = AssociatedObject.Text.IndexOf(DecimalSeparator, StringComparison.CurrentCulture);

        switch (e.Key)
        {
            case Key.Back:
                if (decimalIndex >= 0 && decimalIndex == AssociatedObject.CaretIndex - 1)
                {
                    AssociatedObject.CaretIndex -= 1;
                    e.Handled = true;
                }
                break;

            case Key.Delete:
                if (decimalIndex >= 0 && decimalIndex == AssociatedObject.CaretIndex)
                {
                    AssociatedObject.CaretIndex += 1;
                    e.Handled = true;
                }
                break;

            case Key.Space:
                if (!IsValidInput(GetText(" ")))
                {
                    Beep();
                    e.Handled = true;
                }
                break;
        }
    }

    private void AssociatedObjectPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (InputMode == TextBoxInputMode.DecimalInput && e.Text == DecimalSeparator)
        {
            var decimalIndex = AssociatedObject.Text.IndexOf(DecimalSeparator, StringComparison.CurrentCulture);
            if (decimalIndex >= 0)
            {
                // Typing a second separator jumps past the existing one instead of inserting.
                AssociatedObject.CaretIndex = decimalIndex + 1;
                e.Handled = true;
                return;
            }
        }

        if (!IsValidInput(GetText(e.Text)))
        {
            Beep();
            e.Handled = true;
        }
    }

    private void Beep()
    {
        if (BeepOnInvalid) System.Media.SystemSounds.Beep.Play();
    }

    /// <summary>The text the box would hold if this input were accepted.</summary>
    private string GetText(string input)
    {
        var txt = AssociatedObject;

        var selectionStart = Math.Min(txt.SelectionStart, txt.Text.Length);
        var selectionLength = Math.Min(txt.SelectionLength, txt.Text.Length - selectionStart);

        var realText = txt.Text.Remove(selectionStart, selectionLength);
        var caretIndex = Math.Min(txt.CaretIndex, realText.Length);

        return realText.Insert(caretIndex, input);
    }

    private bool IsValidInput(string input)
    {
        switch (InputMode)
        {
            case TextBoxInputMode.None:
                return true;

            case TextBoxInputMode.DigitInput:
                return input.All(char.IsDigit);

            case TextBoxInputMode.DecimalInput:
                // More than one decimal separator.
                if (input.IndexOf(DecimalSeparator, StringComparison.CurrentCulture) !=
                    input.LastIndexOf(DecimalSeparator, StringComparison.CurrentCulture))
                {
                    return false;
                }

                if (input.Contains('-'))
                {
                    if (JustPositiveDecimalInput) return false;
                    if (input.IndexOf('-') > 0) return false;
                    if (input.Count(c => c == '-') > 1) return false;
                    if (input.Length == 1) return true;
                }

                return decimal.TryParse(input, ValidNumberStyles, CultureInfo.CurrentCulture, out _);

            default:
                throw new InvalidOperationException($"Unknown InputMode - {InputMode}");
        }
    }
}
