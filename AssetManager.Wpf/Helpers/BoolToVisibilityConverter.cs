using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetManager.Helpers;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverse { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is true;
        return IsInverse
            ? (flag ? Visibility.Collapsed : Visibility.Visible)
            : (flag ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visibility = value as Visibility? ?? Visibility.Collapsed;
        return IsInverse
            ? visibility != Visibility.Visible
            : visibility == Visibility.Visible;
    }
}
