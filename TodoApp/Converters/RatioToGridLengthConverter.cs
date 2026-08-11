using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TodoApp.Styles;

namespace TodoApp.Converters;

/// <summary>
/// 0~1の比率をStar単位のGridLengthに変換するコンバーター
///
/// ConverterParameter="Invert"を指定すると(1-比率)を使う。
/// 対になる2つのRow/Columnの両方の表示に使うが、値の反映はViewからViewModelへの
/// 一方向のみ(ConvertBackは使わない)。GridSplitterのドラッグ結果はMainWindow.xaml.cs側で
/// 実際のピクセルサイズから直接比率を計算してViewModelへ書き戻す。
/// (GridSplitterはドラッグ中にRowDefinition/ColumnDefinitionのStar値を直接書き換えるため、
///  そちらをTwoWayバインドの起点にすると書き換え同士が競合してドラッグ操作が破綻する)
/// </summary>
public class RatioToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = ClampRatio(ToRatio(value));
        if (IsInvert(parameter))
            ratio = 1 - ratio;

        return new GridLength(ratio, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("比率の書き戻しはMainWindow.xaml.cs側で行う。");

    private static bool IsInvert(object? parameter) => (parameter as string) == "Invert";

    private static double ClampRatio(double ratio) =>
        double.IsFinite(ratio) ? System.Math.Clamp(ratio, LayoutConstants.MinSplitRatio, LayoutConstants.MaxSplitRatio) : 0.5;

    private static double ToRatio(object? value) => value is double d && double.IsFinite(d) ? d : 0.5;
}
