using System.Windows.Controls;

namespace TodoApp.Views;

/// <summary>
/// NOTE本文の上にNOTE一覧タブを表示する共通コントロール
///
/// DataContextにTodoNodeViewModelを設定して使う。
/// 状況②(子TODO0件時)と状況③のNOTE表示(右側)で使い回す。
/// </summary>
public partial class NoteTabPanel : UserControl
{
    public NoteTabPanel()
    {
        InitializeComponent();
    }
}
