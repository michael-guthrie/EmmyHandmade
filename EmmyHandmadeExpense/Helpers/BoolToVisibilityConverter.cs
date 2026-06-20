namespace AssetManager.Helpers
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool IsInverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bVal = (bool)value;
            return IsInverse
                ? (bVal ? Visibility.Collapsed : Visibility.Visible)
                : (bVal ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vVal = (Visibility)value;
            return IsInverse
                ? (vVal != Visibility.Visible)
                : (vVal == Visibility.Visible);
        }
    }
}
