using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// TODOツリーの変更を監視し、変更があったTodoItemを追跡する
///
/// 変更のあった項目を定期的にDrainし、NodeSyncTaskへ変換してTodoCommandFileServiceへ渡す想定。
/// </summary>
public class TodoChangeTracker
{
    private readonly Dictionary<TodoItem, TodoItem?> _parentOf = [];
    private readonly HashSet<TodoItem> _dirtyNodes = [];
    private readonly string _rootParentDir;

    public TodoChangeTracker(string rootParentDir)
    {
        _rootParentDir = rootParentDir;
    }

    /// <summary>
    /// ツリー全体の監視を開始する
    ///
    /// ルート自身も初回同期対象としてdirty化する。
    /// </summary>
    public void Attach(TodoItem root) => AttachNode(root, parent: null);

    /// <summary>
    /// dirtyな項目を取り出し、NodeSyncTaskへ変換して返す
    ///
    /// 親→子の順で処理されるよう、木の深さ順に並べる。
    /// </summary>
    public IReadOnlyList<NodeSyncTask> DrainSyncTasks()
    {
        if (_dirtyNodes.Count == 0)
        {
            return [];
        }

        var nodes = _dirtyNodes.OrderBy(Depth).ToList();
        _dirtyNodes.Clear();
        return nodes.Select(BuildTask).ToList();
    }

    private NodeSyncTask BuildTask(TodoItem item)
    {
        var parent = _parentOf.GetValueOrDefault(item);
        var ordinal = parent is null ? 1 : parent.ChildTodoList.IndexOf(item) + 1;

        var memos = item.MemoList
            .Select((memo, index) => (TodoFileNaming.BuildMemoFileName(index + 1, memo.Title), memo.Body))
            .ToList();

        var children = item.ChildTodoList
            .Select((child, index) => (child, index + 1, child.Title, child.Status))
            .ToList();

        var saveInfo = new TodoSaveInfo(
            item.SelectedTabIndex,
            item.SelectedChildTodo is null ? null : item.ChildTodoList.IndexOf(item.SelectedChildTodo),
            item.SelectedMemo is null ? null : item.MemoList.IndexOf(item.SelectedMemo));

        return new NodeSyncTask(item, parent, _rootParentDir, ordinal, item.Title, item.Status, item.Body, memos, children, saveInfo);
    }

    private int Depth(TodoItem item)
    {
        var depth = 0;
        for (var current = _parentOf.GetValueOrDefault(item); current is not null; current = _parentOf.GetValueOrDefault(current))
        {
            depth++;
        }

        return depth;
    }

    private void AttachNode(TodoItem item, TodoItem? parent)
    {
        _parentOf[item] = parent;
        _dirtyNodes.Add(item);

        item.PropertyChanged += OnTodoPropertyChanged;

        item.MemoList.CollectionChanged += (_, e) => OnMemoListChanged(item, e);
        foreach (var memo in item.MemoList)
        {
            AttachMemo(item, memo);
        }

        item.ChildTodoList.CollectionChanged += (_, e) => OnChildListChanged(item, e);
        foreach (var child in item.ChildTodoList)
        {
            AttachNode(child, item);
        }
    }

    private void OnTodoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is TodoItem item &&
            e.PropertyName is nameof(TodoItem.Title) or nameof(TodoItem.Body) or nameof(TodoItem.Status)
                or nameof(TodoItem.SelectedTabIndex) or nameof(TodoItem.SelectedChildTodo) or nameof(TodoItem.SelectedMemo))
        {
            _dirtyNodes.Add(item);
        }
    }

    private void AttachMemo(TodoItem owner, MemoItem memo)
    {
        memo.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MemoItem.Title) or nameof(MemoItem.Body))
            {
                _dirtyNodes.Add(owner);
            }
        };
    }

    private void OnMemoListChanged(TodoItem owner, NotifyCollectionChangedEventArgs e)
    {
        _dirtyNodes.Add(owner);
        if (e.NewItems is null)
        {
            return;
        }

        foreach (MemoItem memo in e.NewItems)
        {
            AttachMemo(owner, memo);
        }
    }

    private void OnChildListChanged(TodoItem owner, NotifyCollectionChangedEventArgs e)
    {
        _dirtyNodes.Add(owner);

        if (e.NewItems is not null)
        {
            foreach (TodoItem child in e.NewItems)
            {
                AttachNode(child, owner);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (TodoItem child in e.OldItems)
            {
                DetachNode(child);
            }
        }
    }

    private void DetachNode(TodoItem item)
    {
        _parentOf.Remove(item);
        _dirtyNodes.Remove(item);
        foreach (var child in item.ChildTodoList)
        {
            DetachNode(child);
        }
    }
}
