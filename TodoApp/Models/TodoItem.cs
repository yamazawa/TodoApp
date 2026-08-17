using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Models.Enums;
using TodoApp.Resources;

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
    // 0:TODO表示、1:NOTE表示。既定はNOTE表示を優先する。
    [ObservableProperty]
    private int _selectedTabIndex = 1;

    [ObservableProperty]
    private TodoItem? _selectedChildTodo;

    [ObservableProperty]
    private MemoItem? _selectedMemo;

    // リストの表示横幅。nullの場合は内容に合わせて自動サイズにする。
    [ObservableProperty]
    private double? _listWidth;

    public ObservableCollection<MemoItem> MemoList { get; } = [];

    public ObservableCollection<TodoItem> ChildTodoList { get; } = [];

    /// <summary>
    /// 孫項目詳細で表示する、子TODOの完了数/総数
    /// </summary>
    public string ChildCountText => $"({ChildTodoList.Count(c => c.Status == TodoStatus.Done)}/{ChildTodoList.Count})";

    public TodoItem()
    {
        ChildTodoList.CollectionChanged += OnChildTodoListChanged;
    }

    /// <summary>
    /// メモ情報「README」を1件持つ、新規TODOを作成する
    ///
    /// メモ情報リストは最低1件を保持するルールのため、
    /// 新規TODOには必ずこのメモを1件付与する。
    /// </summary>
    public static TodoItem CreateNew()
    {
        var item = new TodoItem();
        item.MemoList.Add(new MemoItem { Title = Strings.MemoTitle_Readme });
        return item;
    }

    // 子TODOを対応中→未対応→完了の順を保った位置に挿入する。
    // 挿入時点で正しい位置に置くことで、CollectionChangedイベント中に
    // Moveを呼ぶ再入エラー(ObservableCollectionの制約)を避ける。
    public void AddChild(TodoItem child)
    {
        var index = ChildTodoList.TakeWhile(existing => StatusPriority(existing) <= StatusPriority(child)).Count();
        ChildTodoList.Insert(index, child);
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
    // 並び替え自体はステータス変更時(OnChildStatusChanged)にのみ行う。
    // ※ChildTodoList自身のCollectionChanged中にMoveを呼ぶと再入エラーになるため、
    //   追加時の並び替えはAddChildでの挿入位置決定に任せる。
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
            OnPropertyChanged(nameof(ChildCountText));
    }

    private void OnChildStatusChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Status))
            return;

        SortChildren();
        OnPropertyChanged(nameof(ChildCountText));
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
