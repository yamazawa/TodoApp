using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    private TodoNodeViewModel? _viewModel;

    public TodoFramePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
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

        ChildFrameHost.Content = new TodoFramePanel
        {
            DataContext = childNode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    // リストの表示横幅を、自分自身(このTODO)の表示用情報として更新する。
    // GridSplitterはドラッグ中に隣接するColumnDefinitionのWidthを直接書き換えるが、
    // その値をそのまま拾うと不正確なため、実際のActualWidthから計算し直す。
    // ドラッグ操作自体には最小幅を設けない(0~合計幅の範囲で自由に変更できる)。
    private void ListDetailSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_viewModel is null)
            return;

        var total = LeftColumn.ActualWidth + RightColumn.ActualWidth;
        var width = System.Math.Clamp(LeftColumn.ActualWidth + e.HorizontalChange, 0, total);
        if (double.IsFinite(width))
            _viewModel.ListWidth = width;
    }

    // ドラッグを終えた位置が、null(自動サイズ)時に計算される幅以上かつ
    // その差が許容範囲内ならnullに丸める。以後も内容(タブ・リスト項目)に
    // 合わせた自動サイズを保つ。
    // 自動サイズより狭める方向のドラッグは丸めない(そちらは意図的な縮小のため)。
    private void ListDetailSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (_viewModel?.ListWidth is not { } currentWidth)
            return;

        ListPanel.Measure(new Size(double.PositiveInfinity, ListPanel.ActualHeight));
        var naturalWidth = ListPanel.DesiredSize.Width;
        var diff = currentWidth - naturalWidth;
        if (diff >= 0 && diff <= LayoutConstants.ListWidthSnapTolerance)
            _viewModel.ListWidth = null;
    }
}
