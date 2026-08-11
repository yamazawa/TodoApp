using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TodoApp.Styles;
using TodoApp.ViewModels;

namespace TodoApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // 境界ドラッグの比率をViewModelへ反映する。
    //
    // GridSplitterはドラッグ中に隣接するRowDefinition/ColumnDefinitionのStar値を
    // 直接書き換えるが、その値をそのままバインドで拾うと(TwoWayバインドの押し戻しと
    // 競合し、比率が発散する不具合があった)、実際のActual{Width,Height}から
    // 比率を計算し直してViewModelへ書き戻す。GridSplitter自身の書き換えは、
    // 次のConvertでの再描画によって上書きされる。
    private void TopBottomSplitter_DragDelta(object sender, DragDeltaEventArgs e) =>
        UpdateRatio(TopRow.ActualHeight, BottomRow.ActualHeight, e.VerticalChange, ratio => ViewModel.TopBottomRatio = ratio);

    private void BottomLeftRightSplitter_DragDelta(object sender, DragDeltaEventArgs e) =>
        UpdateRatio(LeftColumn.ActualWidth, RightColumn.ActualWidth, e.HorizontalChange, ratio => ViewModel.BottomLeftRightRatio = ratio);

    private void NestedTopBottomSplitter_DragDelta(object sender, DragDeltaEventArgs e) =>
        UpdateRatio(NestedTopRow.ActualHeight, NestedBottomRow.ActualHeight, e.VerticalChange, ratio => ViewModel.NestedTopBottomRatio = ratio);

    private void NestedLeftRightSplitter_DragDelta(object sender, DragDeltaEventArgs e) =>
        UpdateRatio(NestedLeftColumn.ActualWidth, NestedRightColumn.ActualWidth, e.HorizontalChange, ratio => ViewModel.NestedLeftRightRatio = ratio);

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // beforeSize/afterSizeはGridSplitterが既に(不正確に)書き換えた後の値だが、
    // 合計自体はドラッグ前後で変わらないので、そこから比率を計算し直せば正しい値になる。
    private static void UpdateRatio(double beforeSize, double afterSize, double delta, Action<double> setRatio)
    {
        var total = beforeSize + afterSize;
        if (total <= 0)
            return;

        var ratio = (beforeSize + delta) / total;
        if (double.IsFinite(ratio))
            setRatio(System.Math.Clamp(ratio, LayoutConstants.MinSplitRatio, LayoutConstants.MaxSplitRatio));
    }
}
