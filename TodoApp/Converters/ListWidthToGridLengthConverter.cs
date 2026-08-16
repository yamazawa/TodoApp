using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// リストの表示横幅(null許容)をGridLengthへ変換するコンバーター
///
/// nullの場合は内容に合わせて自動サイズ(Auto)にする。
/// </summary>
public class ListWidthToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double width && double.IsFinite(width) ? new GridLength(width, GridUnitType.Pixel) : GridLength.Auto;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
