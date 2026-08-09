using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Models.Enums;

namespace TodoApp.Models;

/// <summary>
/// 1件のTODOを表すモデル。子TODO・メモ情報を持つ。
/// </summary>
public partial class TodoItem : TitledItem
{
    [ObservableProperty]
    private TodoStatus _status = TodoStatus.NotStarted;

    public ObservableCollection<MemoItem> MemoList { get; } = [];

    public ObservableCollection<TodoItem> ChildTodoList { get; } = [];
}
