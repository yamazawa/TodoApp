using CommunityToolkit.Mvvm.ComponentModel;

namespace TodoApp.Models;

/// <summary>
/// メモ情報。
/// </summary>
public partial class MemoItem : TitledItem
{
    [ObservableProperty]
    private string _body = string.Empty;
}
