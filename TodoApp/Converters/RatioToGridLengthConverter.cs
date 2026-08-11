using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters;

/// <summary>
/// 0~1の比率をStar単位のGridLengthに変換するコンバーター
///
/// ConverterParameter="Invert"を指定すると(1-比率)を使う。
/// 対になる2つのRow/Columnのうち、片方だけをMode=TwoWayでバインドし、
/// GridSplitterのドラッグ結果を比率として反映する。もう片方はOneWayのままにする。
/// (両側をTwoWayにすると、GridSplitterによる両側同時書き換えとバインドの
///  押し戻しが競合し、比率が0近くまで暴走する不具合があったため)
/// TwoWay側の値が変わるたびにOneWay側はConvertで再計算されるので、
/// GridSplitterがOneWay側に直接書き込んだ値は上書きされ、比率の合計は常に1に保たれる。
/// </summary>
public class RatioToGridLengthConverter : IValueConverter
{
    private const double MinRatio = 0.05;
    private const double MaxRatio = 1 - MinRatio;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = ClampRatio(ToRatio(value));
        if (IsInvert(parameter))
            ratio = 1 - ratio;

        return new GridLength(ratio, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GridLength gridLength)
            return 0.5;

        var ratio = gridLength.Value;
        if (IsInvert(parameter))
            ratio = 1 - ratio;

        return ClampRatio(ratio);
    }

    private static bool IsInvert(object? parameter) => (parameter as string) == "Invert";

    private static double ClampRatio(double ratio) =>
        double.IsFinite(ratio) ? System.Math.Clamp(ratio, MinRatio, MaxRatio) : 0.5;

    private static double ToRatio(object? value) => value is double d && double.IsFinite(d) ? d : 0.5;
}
