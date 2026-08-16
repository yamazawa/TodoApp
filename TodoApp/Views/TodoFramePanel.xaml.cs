using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

        ChildFrameHost.Content = new TodoFramePanel { DataContext = childNode };
    }
}
