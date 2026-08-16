using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TodoApp.Styles;
using TodoApp.ViewModels;

namespace TodoApp.Views;

/// <summary>
/// 1件のTODOに対する「リスト+詳細」表示を担う、自己参照可能なパネル
///
/// TODOリスト選択時は、選択中の子TODOを同じパネルで再帰的に表示する。
/// </summary>
public partial class TodoFramePanel : UserControl
{
    // ColumnDefinitionはビジュアルツリーに属さないためRelativeSourceで
    // MainViewModelへ直接到達できない。DPとして親から伝播させる。
    public static readonly DependencyProperty BottomLeftRightRatioProperty = DependencyProperty.Register(
        nameof(BottomLeftRightRatio), typeof(double), typeof(TodoFramePanel), new PropertyMetadata(0.2));

    public TodoFramePanel()
    {
        InitializeComponent();
    }

    public double BottomLeftRightRatio
    {
        get => (double)GetValue(BottomLeftRightRatioProperty);
        set => SetValue(BottomLeftRightRatioProperty, value);
    }

    // 境界ドラッグの比率は、再帰の深さに関わらず1つの設定(MainViewModel)を共有する。
    // 計算方法はMainWindow.xaml.csと同様、ActualWidthから比率を計算し直す。
    private void ListDetailSplitter_DragDelta(object sender, DragDeltaEventArgs e) =>
        UpdateRatio(LeftColumn.ActualWidth, RightColumn.ActualWidth, e.HorizontalChange, ratio => MainViewModel.BottomLeftRightRatio = ratio);

    private MainViewModel MainViewModel => (MainViewModel)Window.GetWindow(this)!.DataContext;

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
