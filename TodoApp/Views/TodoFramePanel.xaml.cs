using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using TodoApp.Styles;
using TodoApp.ViewModels;

namespace TodoApp.Views;

/// <summary>
/// 1件のTODOに対する「リスト+詳細」表示を担う、自己参照可能なパネル
///
/// TODOリスト選択時は、選択中の子TODOを同じパネルで再帰的に表示する。
/// 子パネルの生成はDispatcherで1サイクル遅らせ、階層ごとに構築タイミングを
/// 分離する(同一スタック内で一気に構築するとスタックオーバーフローの危険があるため)。
/// </summary>
public partial class TodoFramePanel : UserControl
{
    // ColumnDefinitionはビジュアルツリーに属さないためRelativeSourceで
    // MainViewModelへ直接到達できない。DPとして親から伝播させる。
    public static readonly DependencyProperty BottomLeftRightRatioProperty = DependencyProperty.Register(
        nameof(BottomLeftRightRatio), typeof(double), typeof(TodoFramePanel), new PropertyMetadata(0.2));

    private TodoNodeViewModel? _viewModel;

    public TodoFramePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public double BottomLeftRightRatio
    {
        get => (double)GetValue(BottomLeftRightRatioProperty);
        set => SetValue(BottomLeftRightRatioProperty, value);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as TodoNodeViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ScheduleChildFrameRebuild();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoNodeViewModel.ChildNode))
            ScheduleChildFrameRebuild();
    }

    private void ScheduleChildFrameRebuild() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RebuildChildFrame);

    private void RebuildChildFrame()
    {
        if (_viewModel?.ChildNode is not { } childNode)
        {
            ChildFrameHost.Content = null;
            return;
        }

        if (ChildFrameHost.Content is TodoFramePanel { DataContext: var current } && ReferenceEquals(current, childNode))
            return;

        var panel = new TodoFramePanel { DataContext = childNode };
        panel.SetBinding(BottomLeftRightRatioProperty, new Binding(nameof(BottomLeftRightRatio)) { Source = this });
        ChildFrameHost.Content = panel;
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
