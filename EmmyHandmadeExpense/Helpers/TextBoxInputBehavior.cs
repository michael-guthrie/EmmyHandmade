namespace AssetManager.Helpers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class TextBoxInputBehavior : Behavior<TextBox>
    {
        const NumberStyles ValidNumberStyles = NumberStyles.AllowDecimalPoint
                                             | NumberStyles.AllowThousands
                                             | NumberStyles.AllowLeadingSign;
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
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                if (!IsValidInput(GetText(pastedText)))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                if (BeepOnInvalid)
                {
                    System.Media.SystemSounds.Beep.Play();
                }
                e.CancelCommand();
            }
        }

        private void AssociatedObjectPreviewKeyDown(object sender, KeyEventArgs e)
        {
            int decimalIndex = AssociatedObject.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, StringComparison.CurrentCulture);
            switch (e.Key)
            {
                case Key.Back:
                    if ((decimalIndex >= 0) && (decimalIndex == (AssociatedObject.CaretIndex - 1)))
                    {
                        AssociatedObject.CaretIndex -= 1;
                        e.Handled = true;
                    }
                    break;
                case Key.Delete:
                    if ((decimalIndex >= 0) && (decimalIndex == AssociatedObject.CaretIndex))
                    {
                        AssociatedObject.CaretIndex += 1;
                        e.Handled = true;
                    }
                    break;
                case Key.Space:
                    if (!IsValidInput(GetText(" ")))
                    {
                        if (BeepOnInvalid)
                        {
                            System.Media.SystemSounds.Beep.Play();
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void AssociatedObjectPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (InputMode == TextBoxInputMode.DecimalInput)
            {
                if (e.Text == CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                {
                    int decimalIndex = AssociatedObject.Text.IndexOf(
                        CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator,
                        StringComparison.CurrentCulture);
                    if (decimalIndex >= 0)
                    {
                        AssociatedObject.CaretIndex = decimalIndex + 1;
                        e.Handled = true;
                        return;
                    }
                }
            }
            if (!IsValidInput(GetText(e.Text)))
            {
                if (BeepOnInvalid)
                {
                    System.Media.SystemSounds.Beep.Play();
                }
                e.Handled = true;
            }
        }

        private string GetText(string input)
        {
            var txt = AssociatedObject;

            int selectionStart = txt.SelectionStart;
            if (txt.Text.Length < selectionStart)
                selectionStart = txt.Text.Length;

            int selectionLength = txt.SelectionLength;
            if (txt.Text.Length < selectionStart + selectionLength)
                selectionLength = txt.Text.Length - selectionStart;

            var realtext = txt.Text.Remove(selectionStart, selectionLength);

            int caretIndex = txt.CaretIndex;
            if (realtext.Length < caretIndex)
                caretIndex = realtext.Length;

            var newtext = realtext.Insert(caretIndex, input);

            return newtext;
        }

        private bool IsValidInput(string input)
        {
            switch (InputMode)
            {
                case TextBoxInputMode.None:
                    return true;
                case TextBoxInputMode.DigitInput:
                    return CheckIsDigit(input);

                case TextBoxInputMode.DecimalInput:
                    if (input.IndexOf(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, StringComparison.CurrentCulture) != 
                        input.LastIndexOf(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, StringComparison.CurrentCulture))
                        return false;

                    if (input.Contains("-"))
                    {
                        if (JustPositiveDecimalInput)
                            return false;

                        if (input.IndexOf("-", StringComparison.Ordinal) > 0)
                            return false;

                        if (input.ToCharArray().Count(x => x == '-') > 1)
                            return false;

                        if (input.Length == 1)
                            return true;
                    }

                    var result = decimal.TryParse(input, ValidNumberStyles, CultureInfo.CurrentCulture, out decimal _);
                    return result;

                default: throw new InvalidOperationException($"Unknown InputMode - {InputMode}");
            }
        }

        private bool CheckIsDigit(string wert)
        {
            return wert.ToCharArray().All(Char.IsDigit);
        }
    }
}
