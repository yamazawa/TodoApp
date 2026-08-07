using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// RadioButtonのIsCheckedとenum値を相互変換するコンバーター。
/// ConverterParameterに比較対象のenum値（文字列）を指定して使う。
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked || parameter is null)
        {
            return Binding.DoNothing;
        }

        return Enum.Parse(targetType, parameter.ToString()!);
    }
}
