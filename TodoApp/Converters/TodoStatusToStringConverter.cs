using System.Globalization;
using System.Windows.Data;
using TodoApp.Models.Enums;
using TodoApp.Resources;

namespace TodoApp.Converters;

/// <summary>
/// TodoStatusを表示用文字列(Strings.resx)に変換するコンバーター。
/// </summary>
public class TodoStatusToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            TodoStatus.NotStarted => Strings.Status_NotStarted,
            TodoStatus.InProgress => Strings.Status_InProgress,
            TodoStatus.Done => Strings.Status_Done,
            _ => string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
