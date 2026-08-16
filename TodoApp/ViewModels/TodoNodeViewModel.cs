using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Models.Enums;
using TodoApp.Resources;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// 1件のTodoItem(ノード)に対する子TODO/メモ情報の操作をまとめたViewModel
///
/// メイン画面のルート、下半分の右側(子TODO表示時)の孫項目パネルの両方から使い回す。
/// 保有者がNodeとは別に生成・破棄する(Nodeそのものの破棄は行わない)。
/// </summary>
public partial class TodoNodeViewModel : ObservableObject, IDisposable
{
    public static IReadOnlyList<TodoStatus> StatusOptions { get; } = Enum.GetValues<TodoStatus>();

    private readonly TodoCommandFileService _commandFileService;

    public TodoItem Node { get; }

    public TodoItem? SelectedChildTodo
    {
        get => Node.SelectedChildTodo;
        set
        {
            if (ReferenceEquals(Node.SelectedChildTodo, value))
                return;

            Node.SelectedChildTodo = value;
            OnPropertyChanged();
            NotifyChildTodoCommands();
        }
    }

    public MemoItem? SelectedMemo
    {
        get => Node.SelectedMemo;
        set
        {
            if (ReferenceEquals(Node.SelectedMemo, value))
                return;

            Node.SelectedMemo = value;
            OnPropertyChanged();
            NotifyMemoCommands();
        }
    }

    public int SelectedTabIndex
    {
        get => Node.SelectedTabIndex;
        set
        {
            if (Node.SelectedTabIndex == value)
                return;

            Node.SelectedTabIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsChildTabSelected));
            OnPropertyChanged(nameof(IsMemoTabSelected));
        }
    }

    public bool IsChildTabSelected => SelectedTabIndex == 0;

    public bool IsMemoTabSelected => SelectedTabIndex == 1;

    // タブ見出し。子TODOは完了数/総数、メモ情報は総数を添える。
    public string ChildTabHeader => $"{Strings.Tab_ChildTodo}({Node.ChildTodoList.Count(c => c.Status == TodoStatus.Done)}/{Node.ChildTodoList.Count})";

    public string MemoTabHeader => $"{Strings.Tab_Memo}({Node.MemoList.Count})";

    public TodoNodeViewModel(TodoItem node, TodoCommandFileService commandFileService)
    {
        Node = node;
        _commandFileService = commandFileService;
        Node.ChildTodoList.CollectionChanged += OnChildTodoListChanged;
        Node.MemoList.CollectionChanged += OnMemoListChanged;
    }

    // このViewModelが追加した購読のみ解除する。Node自体の破棄は保有者の責務。
    public void Dispose()
    {
        Node.ChildTodoList.CollectionChanged -= OnChildTodoListChanged;
        Node.MemoList.CollectionChanged -= OnMemoListChanged;
        GC.SuppressFinalize(this);
    }

    private void OnChildTodoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ChildTabHeader));
        NotifyChildTodoCommands();
    }

    private void OnMemoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MemoTabHeader));
        NotifyMemoCommands();
    }

    private void NotifyChildTodoCommands()
    {
        AddChildTodoCommand.NotifyCanExecuteChanged();
        DeleteChildTodoCommand.NotifyCanExecuteChanged();
        MoveChildTodoUpCommand.NotifyCanExecuteChanged();
        MoveChildTodoDownCommand.NotifyCanExecuteChanged();
        ConvertChildTodoToMemoCommand.NotifyCanExecuteChanged();
        OpenChildTodoCommand.NotifyCanExecuteChanged();
    }

    private void NotifyMemoCommands()
    {
        AddMemoCommand.NotifyCanExecuteChanged();
        DeleteMemoCommand.NotifyCanExecuteChanged();
        MoveMemoUpCommand.NotifyCanExecuteChanged();
        MoveMemoDownCommand.NotifyCanExecuteChanged();
        ConvertMemoToChildTodoCommand.NotifyCanExecuteChanged();
        OpenMemoCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddChildTodo))]
    private void AddChildTodo()
    {
        var item = new TodoItem { IsEditing = true };
        Node.AddChild(item);
        SelectedChildTodo = item;
    }

    private bool CanAddChildTodo() => Node.ChildTodoList.Count < TodoFileNaming.MaxItemCount;

    [RelayCommand(CanExecute = nameof(CanDeleteChildTodo))]
    private void DeleteChildTodo() =>
        DeleteWithConfirm(SelectedChildTodo, Node.ChildTodoList, Strings.ConfirmDelete_ChildTodoMessage);

    private bool CanDeleteChildTodo() => SelectedChildTodo is not null;

    [RelayCommand(CanExecute = nameof(CanAddMemo))]
    private void AddMemo()
    {
        var item = new MemoItem { IsEditing = true };
        Node.MemoList.Add(item);
        SelectedMemo = item;
    }

    private bool CanAddMemo() => Node.MemoList.Count < TodoFileNaming.MaxItemCount;

    [RelayCommand(CanExecute = nameof(CanDeleteMemo))]
    private void DeleteMemo() =>
        DeleteWithConfirm(SelectedMemo, Node.MemoList, Strings.ConfirmDelete_MemoMessage);

    private bool CanDeleteMemo() => SelectedMemo is not null;

    [RelayCommand(CanExecute = nameof(CanOpenChildTodo))]
    private void OpenChildTodo()
    {
        if (SelectedChildTodo is not { } child)
            return;

        ExplorerLauncher.OpenFolder(_commandFileService.TryGetPath(child));
    }

    private bool CanOpenChildTodo() => SelectedChildTodo is not null;

    [RelayCommand(CanExecute = nameof(CanOpenMemo))]
    private void OpenMemo()
    {
        if (SelectedMemo is not { } memo)
            return;

        if (_commandFileService.TryGetPath(Node) is not { } folderPath)
            return;

        var ordinal = Node.MemoList.IndexOf(memo) + 1;
        var fileName = TodoFileNaming.BuildMemoFileName(ordinal, memo.Title);
        ExplorerLauncher.SelectFile(Path.Combine(folderPath, fileName));
    }

    private bool CanOpenMemo() => SelectedMemo is not null;

    [RelayCommand(CanExecute = nameof(CanMoveChildTodoUp))]
    private void MoveChildTodoUp() => MoveChildTodo(-1);

    private bool CanMoveChildTodoUp() => CanMoveChildTodo(-1);

    [RelayCommand(CanExecute = nameof(CanMoveChildTodoDown))]
    private void MoveChildTodoDown() => MoveChildTodo(1);

    private bool CanMoveChildTodoDown() => CanMoveChildTodo(1);

    [RelayCommand(CanExecute = nameof(CanMoveMemoUp))]
    private void MoveMemoUp() => Move(Node.MemoList, SelectedMemo, -1);

    private bool CanMoveMemoUp() => CanMove(Node.MemoList, SelectedMemo, -1);

    [RelayCommand(CanExecute = nameof(CanMoveMemoDown))]
    private void MoveMemoDown() => Move(Node.MemoList, SelectedMemo, 1);

    private bool CanMoveMemoDown() => CanMove(Node.MemoList, SelectedMemo, 1);

    // 子TODOをメモ情報へ変換する。孫以下も再帰的に平坦化して全てメモにする。
    [RelayCommand(CanExecute = nameof(CanConvertChildTodoToMemo))]
    private void ConvertChildTodoToMemo()
    {
        if (SelectedChildTodo is not { } item)
            return;

        var newMemos = FlattenToMemos(item);
        Node.ChildTodoList.Remove(item);
        foreach (var memo in newMemos)
        {
            Node.MemoList.Add(memo);
        }

        SelectedTabIndex = 1;
        SelectedMemo = newMemos[0];
    }

    private bool CanConvertChildTodoToMemo() => SelectedChildTodo is not null;

    // メモ情報を子TODOへ変換する。
    [RelayCommand(CanExecute = nameof(CanConvertMemoToChildTodo))]
    private void ConvertMemoToChildTodo()
    {
        if (SelectedMemo is not { } memo)
            return;

        var newItem = new TodoItem { Title = memo.Title, Body = memo.Body };
        Node.MemoList.Remove(memo);
        Node.AddChild(newItem);

        SelectedTabIndex = 0;
        SelectedChildTodo = newItem;
    }

    private bool CanConvertMemoToChildTodo() => SelectedMemo is not null;

    // 自分自身→自分のメモ情報→各子TODO(再帰的に平坦化)の順でメモ化する。
    private static List<MemoItem> FlattenToMemos(TodoItem item)
    {
        var result = new List<MemoItem> { new() { Title = item.Title, Body = item.Body } };
        result.AddRange(item.MemoList.Select(memo => new MemoItem { Title = memo.Title, Body = memo.Body }));
        foreach (var child in item.ChildTodoList)
        {
            result.AddRange(FlattenToMemos(child));
        }

        return result;
    }

    // 子TODOは自動ソート優先のため、ステータスが同じ項目同士でしか入れ替えない。
    private void MoveChildTodo(int delta)
    {
        var list = Node.ChildTodoList;
        if (TryGetMoveIndices(list, SelectedChildTodo, delta) is not { } indices)
            return;

        if (list[indices.OldIndex].Status != list[indices.NewIndex].Status)
            return;

        list.Move(indices.OldIndex, indices.NewIndex);
    }

    private bool CanMoveChildTodo(int delta)
    {
        var list = Node.ChildTodoList;
        if (TryGetMoveIndices(list, SelectedChildTodo, delta) is not { } indices)
            return false;

        return list[indices.OldIndex].Status == list[indices.NewIndex].Status;
    }

    private static void Move<T>(ObservableCollection<T> list, T? item, int delta)
    {
        if (TryGetMoveIndices(list, item, delta) is not { } indices)
            return;

        list.Move(indices.OldIndex, indices.NewIndex);
    }

    private static bool CanMove<T>(ObservableCollection<T> list, T? item, int delta) =>
        TryGetMoveIndices(list, item, delta) is not null;

    private static (int OldIndex, int NewIndex)? TryGetMoveIndices<T>(ObservableCollection<T> list, T? item, int delta)
    {
        if (item is null)
            return null;

        var oldIndex = list.IndexOf(item);
        var newIndex = oldIndex + delta;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= list.Count)
            return null;

        return (oldIndex, newIndex);
    }

    private static void DeleteWithConfirm<T>(T? item, ObservableCollection<T> list, string message)
    {
        if (item is null)
            return;

        var result = MessageBox.Show(message, Strings.ConfirmDelete_Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            list.Remove(item);
    }
}
