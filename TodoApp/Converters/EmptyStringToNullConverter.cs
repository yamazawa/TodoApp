using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// タイトル欄のTextBox.Textとnull許容文字列を橋渡しするコンバーター
///
/// 空文字はnullとして扱う(タイトル未入力時はnullに戻す仕様のため)。
/// </summary>
public class EmptyStringToNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value as string ?? string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? null : value;
}
