using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Models.Enums;

namespace TodoApp.Models;

/// <summary>
/// 1件のTODOを表すモデル
///
/// 子TODO・メモ情報を持つ。
/// </summary>
public partial class TodoItem : TitledItem
{
    private bool _disposed;

    [ObservableProperty]
    private TodoStatus _status = TodoStatus.NotStarted;

    // 以下3つは表示用情報(④保存情報でTODO単位に保存する)。
    // 0:子TODOタブ、1:メモ情報タブ
    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private TodoItem? _selectedChildTodo;

    [ObservableProperty]
    private MemoItem? _selectedMemo;

    public ObservableCollection<MemoItem> MemoList { get; } = [];

    public ObservableCollection<TodoItem> ChildTodoList { get; } = [];

    public TodoItem()
    {
        ChildTodoList.CollectionChanged += OnChildTodoListChanged;
    }

    // 保有者(自分)が子TODOを破棄する。
    // 子孫のイベント購読も含めて全て解放する。
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ChildTodoList.CollectionChanged -= OnChildTodoListChanged;

        foreach (var child in ChildTodoList)
        {
            child.PropertyChanged -= OnChildStatusChanged;
            child.Dispose();
        }

        foreach (var memo in MemoList)
        {
            memo.Dispose();
        }

        base.Dispose();
    }

    // 子TODOは対応中→未対応→完了の順で自動ソートする。
    // 追加された子/ステータス変更を監視し、都度並び替える。
    // 削除された子は自分が保有者として破棄する。
    private void OnChildTodoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        e.ForEachAddedRemoved<TodoItem>(
            added => added.PropertyChanged += OnChildStatusChanged,
            removed => removed.PropertyChanged -= OnChildStatusChanged);

        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (TodoItem removed in e.OldItems)
            {
                removed.Dispose();
            }
        }

        if (e.Action != NotifyCollectionChangedAction.Move)
            SortChildren();
    }

    private void OnChildStatusChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Status))
            return;

        SortChildren();
    }

    private void SortChildren()
    {
        var sorted = ChildTodoList.OrderBy(StatusPriority).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = ChildTodoList.IndexOf(sorted[i]);
            if (currentIndex == i)
                continue;

            ChildTodoList.Move(currentIndex, i);
        }
    }

    private static int StatusPriority(TodoItem item) => item.Status switch
    {
        TodoStatus.InProgress => 0,
        TodoStatus.NotStarted => 1,
        TodoStatus.Done => 2,
        _ => 3,
    };
}
