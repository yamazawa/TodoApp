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
/// 自己参照可能で、選択中の子TODOをChildNodeとして持つ(TodoFramePanelでの再帰表示用)。
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
            RebuildChildNode();
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
            OnPropertyChanged(nameof(ShowMemoDetail));
            OnPropertyChanged(nameof(ShowChildFrame));
        }
    }

    public bool IsChildTabSelected => SelectedTabIndex == 0;

    public bool IsMemoTabSelected => SelectedTabIndex == 1;

    // リスト表示の簡略化(状況①②③)の判定に使う。
    // 状況① : 子TODO0件、メモ1件 → リスト自体を表示しない。
    public bool HasChildTodo => Node.ChildTodoList.Count > 0;

    public bool HasMultipleMemos => Node.MemoList.Count > 1;

    public bool IsListless => !HasChildTodo && !HasMultipleMemos;

    public bool HasList => !IsListless;

    // 状況② : 子TODO0件、メモ2件以上 → タブ無しでメモ情報リストのみを表示する。
    public bool IsMemoListOnly => !HasChildTodo;

    // 右側(詳細)の表示切り替え。状況②は常にメモ、状況③は選択中タブに従う。
    public bool ShowMemoDetail => IsMemoListOnly || IsMemoTabSelected;

    public bool ShowChildFrame => HasChildTodo && IsChildTabSelected;

    // 選択中の子TODOをラップしたノード。TODOリスト選択時の再帰表示(TodoFramePanel)に使う。
    [ObservableProperty]
    private TodoNodeViewModel? _childNode;

    // タブ見出し。子TODOは完了数/総数、メモ情報は総数を添える。
    public string ChildTabHeader => $"{Strings.Tab_TodoList}({Node.ChildTodoList.Count(c => c.Status == TodoStatus.Done)}/{Node.ChildTodoList.Count})";

    public string MemoTabHeader => $"{Strings.Tab_NoteList}({Node.MemoList.Count})";

    public TodoNodeViewModel(TodoItem node, TodoCommandFileService commandFileService)
    {
        Node = node;
        _commandFileService = commandFileService;
        Node.ChildTodoList.CollectionChanged += OnChildTodoListChanged;
        Node.MemoList.CollectionChanged += OnMemoListChanged;
        RebuildChildNode();
    }

    // このViewModelが追加した購読・ChildNodeを解放する。Node自体の破棄は保有者の責務。
    public void Dispose()
    {
        Node.ChildTodoList.CollectionChanged -= OnChildTodoListChanged;
        Node.MemoList.CollectionChanged -= OnMemoListChanged;
        ChildNode?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnChildTodoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ChildTabHeader));
        OnPropertyChanged(nameof(HasChildTodo));
        OnPropertyChanged(nameof(IsListless));
        OnPropertyChanged(nameof(HasList));
        OnPropertyChanged(nameof(IsMemoListOnly));
        OnPropertyChanged(nameof(ShowMemoDetail));
        OnPropertyChanged(nameof(ShowChildFrame));
        NotifyChildTodoCommands();
    }

    private void OnMemoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MemoTabHeader));
        OnPropertyChanged(nameof(HasMultipleMemos));
        OnPropertyChanged(nameof(IsListless));
        OnPropertyChanged(nameof(HasList));
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

    // 状況①からTODOリストを追加し、状況③へ遷移する。追加項目をすぐ編集できるようタブを合わせる。
    [RelayCommand(CanExecute = nameof(CanAddChildTodo))]
    private void AddChildTodo()
    {
        var item = TodoItem.CreateNew();
        item.IsEditing = true;
        Node.AddChild(item);
        SelectedTabIndex = 0;
        SelectedChildTodo = item;
    }

    private bool CanAddChildTodo() => Node.ChildTodoList.Count < TodoFileNaming.MaxItemCount;

    // 右クリックメニューからのステータス変更。未対応→対応中→完了の順に1段階だけ進める。
    [RelayCommand]
    private void AdvanceChildTodoStatus(TodoItem item)
    {
        item.Status = item.Status switch
        {
            TodoStatus.NotStarted => TodoStatus.InProgress,
            TodoStatus.InProgress => TodoStatus.Done,
            _ => item.Status,
        };
    }

    [RelayCommand(CanExecute = nameof(CanDeleteChildTodo))]
    private void DeleteChildTodo()
    {
        if (DeleteWithConfirm(SelectedChildTodo, Node.ChildTodoList, Strings.ConfirmDelete_ChildTodoMessage))
            SelectedChildTodo = null;
    }

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
    private void DeleteMemo()
    {
        if (DeleteWithConfirm(SelectedMemo, Node.MemoList, Strings.ConfirmDelete_MemoMessage))
            SelectedMemo = null;
    }

    // メモ情報リストは最低1件を保持するため、2件以上のときのみ削除できる。
    private bool CanDeleteMemo() => SelectedMemo is not null && Node.MemoList.Count > 1;

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
        SelectedChildTodo = null;
        foreach (var memo in newMemos)
        {
            Node.MemoList.Add(memo);
        }

        SelectedTabIndex = 1;
        SelectedMemo = newMemos[0];
    }

    private bool CanConvertChildTodoToMemo() => SelectedChildTodo is not null;

    // メモ情報を子TODOへ変換する。メモ情報自体を新しいTODOの初期メモとして引き継ぐ。
    [RelayCommand(CanExecute = nameof(CanConvertMemoToChildTodo))]
    private void ConvertMemoToChildTodo()
    {
        if (SelectedMemo is not { } memo)
            return;

        var newItem = new TodoItem { Title = memo.Title };
        Node.MemoList.Remove(memo);
        SelectedMemo = null;
        newItem.MemoList.Add(memo);
        Node.AddChild(newItem);

        SelectedTabIndex = 0;
        SelectedChildTodo = newItem;
    }

    private bool CanConvertMemoToChildTodo() => SelectedMemo is not null && Node.MemoList.Count > 1;

    // 自分のメモ情報→各子TODO(再帰的に平坦化)の順でメモ化する。
    private static List<MemoItem> FlattenToMemos(TodoItem item)
    {
        var result = item.MemoList.Select(memo => new MemoItem { Title = memo.Title, Body = memo.Body }).ToList();
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

    private static bool DeleteWithConfirm<T>(T? item, ObservableCollection<T> list, string message)
    {
        if (item is null)
            return false;

        var result = MessageBox.Show(message, Strings.ConfirmDelete_Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return false;

        list.Remove(item);
        return true;
    }

    // TODOリスト選択時の再帰表示(TodoFramePanel)用に、選択中の子TODOをラップし直す。
    private void RebuildChildNode()
    {
        ChildNode?.Dispose();
        ChildNode = SelectedChildTodo is { } child ? new TodoNodeViewModel(child, _commandFileService) : null;
    }
}
