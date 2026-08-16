using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TodoApp.Behaviors;

/// <summary>
/// GridSplitterのドラッグで、隣接するColumnDefinitionの横幅(px)を追跡するビヘイビア
///
/// ドラッグ中はActualWidthから幅を計算し直してWidthへ書き戻す。
/// ドラッグ終了時、MeasureElementの自動サイズ(無限幅で測った幅)以上かつ
/// その差がSnapTolerance以内ならWidthをnullに戻す(自動サイズへ切り替える)。
/// 自動サイズより狭める方向のドラッグは丸めない(そちらは意図的な縮小のため)。
/// </summary>
public static class SplitterColumnWidthBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SplitterColumnWidthBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty TargetColumnProperty =
        DependencyProperty.RegisterAttached("TargetColumn", typeof(ColumnDefinition), typeof(SplitterColumnWidthBehavior));

    public static readonly DependencyProperty SiblingColumnProperty =
        DependencyProperty.RegisterAttached("SiblingColumn", typeof(ColumnDefinition), typeof(SplitterColumnWidthBehavior));

    public static readonly DependencyProperty MeasureElementProperty =
        DependencyProperty.RegisterAttached("MeasureElement", typeof(FrameworkElement), typeof(SplitterColumnWidthBehavior));

    public static readonly DependencyProperty SnapToleranceProperty =
        DependencyProperty.RegisterAttached("SnapTolerance", typeof(double), typeof(SplitterColumnWidthBehavior), new PropertyMetadata(0d));

    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.RegisterAttached(
            "Width", typeof(double?), typeof(SplitterColumnWidthBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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

    public static void SetWidth(DependencyObject obj, double? value) => obj.SetValue(WidthProperty, value);

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

        measureElement.Measure(new Size(double.PositiveInfinity, measureElement.ActualHeight));
        var diff = currentWidth - measureElement.DesiredSize.Width;
        if (diff >= 0 && diff <= GetSnapTolerance(splitter))
            SetWidth(splitter, null);
    }
}
