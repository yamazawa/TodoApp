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
    private void ListDetailSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_viewModel is null)
            return;

        var total = LeftColumn.ActualWidth + RightColumn.ActualWidth;
        var maxWidth = System.Math.Max(LayoutConstants.MinSplitPaneWidth, total - LayoutConstants.MinSplitPaneWidth);
        var width = System.Math.Clamp(LeftColumn.ActualWidth + e.HorizontalChange, LayoutConstants.MinSplitPaneWidth, maxWidth);
        if (double.IsFinite(width))
            _viewModel.ListWidth = width;
    }
}
