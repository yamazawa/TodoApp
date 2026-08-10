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
public class TodoChangeTracker : IDisposable
{
    private readonly Dictionary<TodoItem, TodoItem?> _parentOf = [];
    private readonly HashSet<TodoItem> _dirtyNodes = [];
    private readonly Dictionary<TodoItem, NotifyCollectionChangedEventHandler> _memoListHandlers = [];
    private readonly Dictionary<TodoItem, NotifyCollectionChangedEventHandler> _childListHandlers = [];
    private readonly Dictionary<MemoItem, PropertyChangedEventHandler> _memoHandlers = [];
    private readonly string _rootParentDir;
    private TodoItem? _root;
    private bool _disposed;

    public TodoChangeTracker(string rootParentDir)
    {
        _rootParentDir = rootParentDir;
    }

    /// <summary>
    /// ツリー全体の監視を開始する
    ///
    /// ルート自身も初回同期対象としてdirty化する。
    /// </summary>
    public void Attach(TodoItem root)
    {
        _root = root;
        AttachNode(root, parent: null);
    }

    /// <summary>
    /// 監視中のツリー内での親を返す
    ///
    /// ルート自身、または監視対象外の場合はnullを返す。
    /// </summary>
    public TodoItem? GetParent(TodoItem item) => _parentOf.GetValueOrDefault(item);

    /// <summary>
    /// dirtyな項目を取り出し、NodeSyncTaskへ変換して返す
    ///
    /// 親→子の順で処理されるよう、木の深さ順に並べる。
    /// </summary>
    public IReadOnlyList<NodeSyncTask> DrainSyncTasks()
    {
        if (_dirtyNodes.Count == 0)
            return [];

        var nodes = _dirtyNodes.OrderBy(Depth).ToList();
        _dirtyNodes.Clear();
        return nodes.Select(BuildTask).ToList();
    }

    // 監視対象のツリーから全て購読を解除する。
    // モデル自体の破棄は行わない(保有者の責務)。
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_root is not null)
        {
            DetachNode(_root);
            _root = null;
        }

        GC.SuppressFinalize(this);
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

        NotifyCollectionChangedEventHandler memoListHandler = (_, e) => OnMemoListChanged(item, e);
        item.MemoList.CollectionChanged += memoListHandler;
        _memoListHandlers[item] = memoListHandler;
        foreach (var memo in item.MemoList)
        {
            AttachMemo(item, memo);
        }

        NotifyCollectionChangedEventHandler childListHandler = (_, e) => OnChildListChanged(item, e);
        item.ChildTodoList.CollectionChanged += childListHandler;
        _childListHandlers[item] = childListHandler;
        foreach (var child in item.ChildTodoList)
        {
            AttachNode(child, item);
        }
    }

    private void OnTodoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TodoItem item)
            return;

        if (e.PropertyName is nameof(TodoItem.Title) or nameof(TodoItem.Body) or nameof(TodoItem.Status)
            or nameof(TodoItem.SelectedTabIndex) or nameof(TodoItem.SelectedChildTodo) or nameof(TodoItem.SelectedMemo))
            _dirtyNodes.Add(item);
    }

    private void AttachMemo(TodoItem owner, MemoItem memo)
    {
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName is nameof(MemoItem.Title) or nameof(MemoItem.Body))
                _dirtyNodes.Add(owner);
        };

        memo.PropertyChanged += handler;
        _memoHandlers[memo] = handler;
    }

    private void DetachMemo(MemoItem memo)
    {
        if (_memoHandlers.Remove(memo, out var handler))
            memo.PropertyChanged -= handler;
    }

    private void OnMemoListChanged(TodoItem owner, NotifyCollectionChangedEventArgs e)
    {
        _dirtyNodes.Add(owner);

        if (e.Action == NotifyCollectionChangedAction.Move)
            return;

        e.ForEachAddedRemoved<MemoItem>(
            added => AttachMemo(owner, added),
            DetachMemo);
    }

    private void OnChildListChanged(TodoItem owner, NotifyCollectionChangedEventArgs e)
    {
        _dirtyNodes.Add(owner);

        if (e.Action == NotifyCollectionChangedAction.Move)
            return;

        e.ForEachAddedRemoved<TodoItem>(
            added => AttachNode(added, owner),
            DetachNode);
    }

    private void DetachNode(TodoItem item)
    {
        _parentOf.Remove(item);
        _dirtyNodes.Remove(item);
        item.PropertyChanged -= OnTodoPropertyChanged;

        if (_memoListHandlers.Remove(item, out var memoListHandler))
            item.MemoList.CollectionChanged -= memoListHandler;

        foreach (var memo in item.MemoList)
        {
            DetachMemo(memo);
        }

        if (_childListHandlers.Remove(item, out var childListHandler))
            item.ChildTodoList.CollectionChanged -= childListHandler;

        foreach (var child in item.ChildTodoList)
        {
            DetachNode(child);
        }
    }
}
