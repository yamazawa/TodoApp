using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TodoApp.Views;

/// <summary>
/// 子TODO/メモ情報のタブ切替リストを表示する共通コントロール
///
/// DataContextにTodoNodeViewModelを設定して使う。
/// メイン画面のルート、下半分の右側(子TODO表示時)の孫項目パネルで使い回す。
/// </summary>
public partial class TodoListPanel : UserControl
{
    // 子TODOの「移動」リンク用。MainViewModel側のコマンドを外部から設定する。
    // ContextMenu(別ツリー)からはPlacementTarget.Tag経由でRootごと参照して辿り着く。
    public static readonly DependencyProperty NavigateToChildCommandProperty = DependencyProperty.Register(
        nameof(NavigateToChildCommand), typeof(ICommand), typeof(TodoListPanel));

    public TodoListPanel()
    {
        InitializeComponent();
    }

    public ICommand? NavigateToChildCommand
    {
        get => (ICommand?)GetValue(NavigateToChildCommandProperty);
        set => SetValue(NavigateToChildCommandProperty, value);
    }
}
