using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// 値がnull(文字列の場合は空文字も含む)ならCollapsed、それ以外はVisibleに変換するコンバーター
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return Visibility.Collapsed;

        if (value is string text && string.IsNullOrEmpty(text))
            return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
