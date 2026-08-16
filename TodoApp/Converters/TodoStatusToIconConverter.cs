using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TodoApp.Models.Enums;

namespace TodoApp.Converters;

/// <summary>
/// TodoStatusを対応するステータスアイコンの画像ソースへ変換する
/// </summary>
public sealed class TodoStatusToIconConverter : IValueConverter
{
    private static readonly BitmapImage NotStartedIcon = Load("未対応.ico");
    private static readonly BitmapImage InProgressIcon = Load("対応中.ico");
    private static readonly BitmapImage DoneIcon = Load("完了.ico");

    private static BitmapImage Load(string fileName) => new(new Uri($"pack://application:,,,/image/{fileName}"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as TodoStatus?) switch
        {
            TodoStatus.InProgress => InProgressIcon,
            TodoStatus.Done => DoneIcon,
            _ => NotStartedIcon,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
