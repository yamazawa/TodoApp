using System.Windows.Controls;

namespace TodoApp.Views;

/// <summary>
/// 子TODO/メモ情報のタブ切替リストを表示する共通コントロール
///
/// DataContextにTodoNodeViewModelを設定して使う。
/// メイン画面のルート、下半分の右側(子TODO表示時)の孫項目パネルで使い回す。
/// </summary>
public partial class TodoListPanel : UserControl
{
    public TodoListPanel()
    {
        InitializeComponent();
    }
}
