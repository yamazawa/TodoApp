using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// 0~1の比率をStar単位のGridLengthに変換するコンバーター
///
/// ConverterParameter="Invert"を指定すると(1-比率)を使う。
/// 対になる2つのRow/Columnにこのコンバーターを両方向で割り当て、
/// GridSplitterのドラッグ結果を比率として双方向にバインドする。
/// </summary>
public class RatioToGridLengthConverter : IValueConverter
{
    private const double MinRatio = 0.05;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = ToRatio(value);
        if (IsInvert(parameter))
            ratio = 1 - ratio;

        return new GridLength(System.Math.Max(ratio, MinRatio), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GridLength gridLength)
            return 0.5;

        var ratio = gridLength.Value;
        if (IsInvert(parameter))
            ratio = 1 - ratio;

        return ratio;
    }

    private static bool IsInvert(object? parameter) => (parameter as string) == "Invert";

    private static double ToRatio(object? value) => value is double d ? d : 0.5;
}
