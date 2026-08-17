using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TodoApp.Views;

/// <summary>
/// 状況③(子TODOが1件以上)のリストを表示する共通コントロール
///
/// 先頭に選択中NOTE、以降に子TODOを順に表示する。
/// DataContextにTodoNodeViewModelを設定して使う。
/// メイン画面のルート、右側(子TODO表示時)の孫項目パネルで使い回す。
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

    // 右クリックした項目を選択状態にする。
    // 削除メニューは選択中の項目に対して動作するため、右クリック時点で選択を合わせる。
    private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item)
            item.IsSelected = true;
    }
}
