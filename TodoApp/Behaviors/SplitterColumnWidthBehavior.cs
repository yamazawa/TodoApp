using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace TodoApp.Behaviors;

/// <summary>
/// GridSplitterのドラッグで、隣接するColumnDefinitionの横幅(px)を追跡するビヘイビア
///
/// ドラッグ中はActualWidthから幅を計算し直してWidthへ書き戻す。
/// ドラッグ終了時、MeasureElementの自動サイズ(無限幅で測った幅)以上かつ
/// その差がSnapTolerance以内ならWidthをnullに戻す(自動サイズへ切り替える)。
/// 自動サイズより狭める方向のドラッグは丸めない(そちらは意図的な縮小のため)。
///
/// TargetColumn.WidthはXAMLの{Binding}では宣言しない。GridSplitterはドラッグ中に
/// ColumnDefinition.WidthへSetValueで直接書き込むため、そちらにバインドを
/// 宣言しているとバインド自体が切れてしまう。代わりにこのビヘイビアが
/// SetCurrentValueで反映することで、GridSplitterの書き込みと共存させる。
/// </summary>
public static class SplitterColumnWidthBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SplitterColumnWidthBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty TargetColumnProperty =
        DependencyProperty.RegisterAttached(
            "TargetColumn", typeof(ColumnDefinition), typeof(SplitterColumnWidthBehavior),
            new PropertyMetadata(null, OnTargetColumnOrWidthChanged));

    public static readonly DependencyProperty SiblingColumnProperty =
        DependencyProperty.RegisterAttached("SiblingColumn", typeof(ColumnDefinition), typeof(SplitterColumnWidthBehavior));

    public static readonly DependencyProperty MeasureElementProperty =
        DependencyProperty.RegisterAttached("MeasureElement", typeof(FrameworkElement), typeof(SplitterColumnWidthBehavior));

    public static readonly DependencyProperty SnapToleranceProperty =
        DependencyProperty.RegisterAttached("SnapTolerance", typeof(double), typeof(SplitterColumnWidthBehavior), new PropertyMetadata(0d));

    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.RegisterAttached(
            "Width", typeof(double?), typeof(SplitterColumnWidthBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTargetColumnOrWidthChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ColumnDefinition? GetTargetColumn(DependencyObject obj) => (ColumnDefinition?)obj.GetValue(TargetColumnProperty);

    public static void SetTargetColumn(DependencyObject obj, ColumnDefinition? value) => obj.SetValue(TargetColumnProperty, value);

    public static ColumnDefinition? GetSiblingColumn(DependencyObject obj) => (ColumnDefinition?)obj.GetValue(SiblingColumnProperty);

    public static void SetSiblingColumn(DependencyObject obj, ColumnDefinition? value) => obj.SetValue(SiblingColumnProperty, value);

    public static FrameworkElement? GetMeasureElement(DependencyObject obj) => (FrameworkElement?)obj.GetValue(MeasureElementProperty);

    public static void SetMeasureElement(DependencyObject obj, FrameworkElement? value) => obj.SetValue(MeasureElementProperty, value);

    public static double GetSnapTolerance(DependencyObject obj) => (double)obj.GetValue(SnapToleranceProperty);

    public static void SetSnapTolerance(DependencyObject obj, double value) => obj.SetValue(SnapToleranceProperty, value);

    public static double? GetWidth(DependencyObject obj) => (double?)obj.GetValue(WidthProperty);

    // SetValueだけだとバインド先(ViewModel)への反映が遅れる/効かないことがあるため、
    // 明示的にUpdateSourceしてその場でバインド元へ反映させる。
    public static void SetWidth(DependencyObject obj, double? value)
    {
        obj.SetValue(WidthProperty, value);
        BindingOperations.GetBindingExpression(obj, WidthProperty)?.UpdateSource();
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GridSplitter splitter)
            return;

        if (e.NewValue is true)
        {
            splitter.DragDelta += OnDragDelta;
            splitter.DragCompleted += OnDragCompleted;
        }
        else
        {
            splitter.DragDelta -= OnDragDelta;
            splitter.DragCompleted -= OnDragCompleted;
        }
    }

    // TargetColumn・Widthのどちらが先に設定されても正しく反映されるよう、
    // 両方の変更をこのハンドラへ集約する。
    private static void OnTargetColumnOrWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (GetTargetColumn(d) is not { } target)
            return;

        var width = GetWidth(d);
        var gridLength = width is { } value && double.IsFinite(value)
            ? new GridLength(value, GridUnitType.Pixel)
            : GridLength.Auto;

        // SetCurrentValueを使うことで、GridSplitterがドラッグ中に行う
        // ColumnDefinition.Widthへの直接書き込みと衝突しないようにする。
        target.SetCurrentValue(ColumnDefinition.WidthProperty, gridLength);
    }

    // GridSplitterはドラッグ中に隣接するColumnDefinitionのWidthを直接書き換えるが、
    // その値をそのまま拾うと不正確なため、実際のActualWidthから計算し直す。
    private static void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var splitter = (GridSplitter)sender;
        if (GetTargetColumn(splitter) is not { } target || GetSiblingColumn(splitter) is not { } sibling)
            return;

        var total = target.ActualWidth + sibling.ActualWidth;
        var width = Math.Clamp(target.ActualWidth + e.HorizontalChange, 0, total);
        if (double.IsFinite(width))
            SetWidth(splitter, width);
    }

    private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var splitter = (GridSplitter)sender;
        if (GetWidth(splitter) is not { } currentWidth || GetMeasureElement(splitter) is not { } measureElement)
            return;

        // Measureを直接呼ぶのは幅を調べるためだけの一時的なもの。
        // 呼びっぱなしだと「この制約で測定済み」の状態が残ることがあるため、
        // 判定後は必ずInvalidateMeasureしてWPF自身の次の計測に委ねる。
        measureElement.Measure(new Size(double.PositiveInfinity, measureElement.ActualHeight));
        var naturalWidth = measureElement.DesiredSize.Width;
        measureElement.InvalidateMeasure();

        var diff = currentWidth - naturalWidth;
        if (diff >= 0 && diff <= GetSnapTolerance(splitter))
            SetWidth(splitter, null);
    }
}
