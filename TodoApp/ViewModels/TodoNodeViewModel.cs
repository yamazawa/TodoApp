using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    // このタイトルのメモ情報を、Claudeセッションの再開情報(本文=セッションID)として扱う。
    private const string ClaudeMemoTitle = "Claude";

    // このタイトルのメモ情報へ、送信した命令文を時刻付きで蓄積する。
    private const string ClaudeHistoryMemoTitle = "Claude履歴";

    private readonly TodoCommandFileService _commandFileService;
    private readonly ClaudeCompletionWatcher _completionWatcher;

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
            OnPropertyChanged(nameof(SelectedEntry));
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

            if (Node.SelectedMemo is { } old)
                old.PropertyChanged -= OnSelectedMemoPropertyChanged;

            Node.SelectedMemo = value;

            if (Node.SelectedMemo is { } updated)
                updated.PropertyChanged += OnSelectedMemoPropertyChanged;

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedEntry));
            OnPropertyChanged(nameof(ListEntries));
            NotifyMemoCommands();
        }
    }

    // 表示中NOTEの本文をClaudeへの命令文として使うため、編集中もボタンの有効/無効を追従させる。
    private void OnSelectedMemoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MemoItem.Body) or nameof(MemoItem.Title))
            SendToClaudeCommand.NotifyCanExecuteChanged();
    }

    // リストの表示横幅。nullの場合は内容に合わせて自動サイズにする(TodoFramePanelが解釈する)。
    public double? ListWidth
    {
        get => Node.ListWidth;
        set
        {
            if (Node.ListWidth == value)
                return;

            Node.ListWidth = value;
            OnPropertyChanged();
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
            OnPropertyChanged(nameof(SelectedEntry));
            MoveChildTodoUpCommand.NotifyCanExecuteChanged();
            MoveChildTodoDownCommand.NotifyCanExecuteChanged();
        }
    }

    // 0:TODO表示中、1:NOTE表示中(タブ廃止後も右側の表示モードとして使う)。既定はNOTEを優先する。
    public bool IsChildTabSelected => SelectedTabIndex == 0;

    public bool IsMemoTabSelected => SelectedTabIndex == 1;

    // リスト表示の簡略化(状況①②③)の判定に使う。
    // 状況① : 子TODO0件、メモ1件 → リスト自体を表示しない。
    public bool HasChildTodo => Node.ChildTodoList.Count > 0;

    public bool HasMultipleMemos => Node.MemoList.Count > 1;

    public bool IsListless => !HasChildTodo && !HasMultipleMemos;

    // 状況② : 子TODO0件、メモ2件以上 → NOTE一覧タブ+本文のみを表示する。
    public bool IsMemoListOnly => !HasChildTodo && HasMultipleMemos;

    // 右側(詳細)の表示切り替え。状況②は常にメモ、状況③は選択中エントリに従う。
    public bool ShowMemoDetail => IsMemoListOnly || IsMemoTabSelected;

    public bool ShowChildFrame => HasChildTodo && IsChildTabSelected;

    // 選択中の子TODOをラップしたノード。TODOリスト選択時の再帰表示(TodoFramePanel)に使う。
    [ObservableProperty]
    private TodoNodeViewModel? _childNode;

    // 状況③のリスト表示。先頭に選択中NOTEを1件、その後にTODOを順に並べる。
    public IEnumerable<object> ListEntries
    {
        get
        {
            if (Node.SelectedMemo is { } memo)
                yield return memo;

            foreach (var child in Node.ChildTodoList)
                yield return child;
        }
    }

    // 状況③のリストの選択項目。NOTEエントリ選択でNOTEモード、TODO項目選択でTODOモードに切り替える。
    public object? SelectedEntry
    {
        get => IsMemoTabSelected ? (object?)Node.SelectedMemo : SelectedChildTodo;
        set
        {
            switch (value)
            {
                case MemoItem:
                    SelectedTabIndex = 1;
                    break;
                case TodoItem todo:
                    SelectedTabIndex = 0;
                    SelectedChildTodo = todo;
                    break;
            }
        }
    }

    public TodoNodeViewModel(TodoItem node, TodoCommandFileService commandFileService, ClaudeCompletionWatcher completionWatcher)
    {
        Node = node;
        _commandFileService = commandFileService;
        _completionWatcher = completionWatcher;
        Node.ChildTodoList.CollectionChanged += OnChildTodoListChanged;
        Node.MemoList.CollectionChanged += OnMemoListChanged;

        if (Node.SelectedMemo is { } memo)
            memo.PropertyChanged += OnSelectedMemoPropertyChanged;

        RebuildChildNode();
    }

    // このViewModelが追加した購読・ChildNodeを解放する。Node自体の破棄は保有者の責務。
    public void Dispose()
    {
        Node.ChildTodoList.CollectionChanged -= OnChildTodoListChanged;
        Node.MemoList.CollectionChanged -= OnMemoListChanged;

        if (Node.SelectedMemo is { } memo)
            memo.PropertyChanged -= OnSelectedMemoPropertyChanged;

        ChildNode?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnChildTodoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasChildTodo));
        OnPropertyChanged(nameof(IsListless));
        OnPropertyChanged(nameof(IsMemoListOnly));
        OnPropertyChanged(nameof(ShowMemoDetail));
        OnPropertyChanged(nameof(ShowChildFrame));
        OnPropertyChanged(nameof(ListEntries));
        NotifyChildTodoCommands();
    }

    private void OnMemoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasMultipleMemos));
        OnPropertyChanged(nameof(IsListless));
        OnPropertyChanged(nameof(IsMemoListOnly));
        OnPropertyChanged(nameof(ShowMemoDetail));
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
        SendToClaudeCommand.NotifyCanExecuteChanged();
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

    // 表示中NOTEの本文を命令文として、Claudeセッションを新規PowerShellウィンドウで起動する。
    //
    // 「Claude」メモ(本文=セッションID)が空なら新規発行し、既にあれば再開する。
    // 送信内容は「Claude履歴」メモへ時刻付きで蓄積し、未対応なら対応中へ進める。
    [RelayCommand(CanExecute = nameof(CanSendToClaude))]
    private void SendToClaude()
    {
        if (SelectedMemo is not { } instructionMemo)
            return;

        if (_commandFileService.TryGetPath(Node) is not { } folderPath)
            return;

        var instruction = instructionMemo.Body;

        var claudeMemo = GetOrCreateMemo(ClaudeMemoTitle);
        var isNewSession = string.IsNullOrWhiteSpace(claudeMemo.Body);
        if (isNewSession)
            claudeMemo.Body = Guid.NewGuid().ToString();

        AppendHistory(instruction);

        if (Node.Status == TodoStatus.NotStarted)
            Node.Status = TodoStatus.InProgress;

        _completionWatcher.Watch(Node, folderPath);
        ClaudeSessionLauncher.Launch(folderPath, claudeMemo.Body, instruction, isNewSession);
    }

    private bool CanSendToClaude()
    {
        if (SelectedMemo is not { } memo)
            return false;

        if (memo.Title is ClaudeMemoTitle or ClaudeHistoryMemoTitle)
            return false;

        if (string.IsNullOrWhiteSpace(memo.Body))
            return false;

        var missingMemoCount = (HasMemo(ClaudeMemoTitle) ? 0 : 1) + (HasMemo(ClaudeHistoryMemoTitle) ? 0 : 1);
        return Node.MemoList.Count + missingMemoCount <= TodoFileNaming.MaxItemCount;
    }

    private bool HasMemo(string title) => Node.MemoList.Any(m => m.Title == title);

    private MemoItem GetOrCreateMemo(string title)
    {
        if (Node.MemoList.FirstOrDefault(m => m.Title == title) is { } existing)
            return existing;

        var created = new MemoItem { Title = title };
        Node.MemoList.Add(created);
        return created;
    }

    // 送信した命令文を時刻付きで「Claude履歴」メモの末尾に追記する。
    private void AppendHistory(string instruction)
    {
        var history = GetOrCreateMemo(ClaudeHistoryMemoTitle);
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {instruction}";
        history.Body = string.IsNullOrEmpty(history.Body) ? entry : $"{history.Body}\n\n{entry}";
    }

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
        newItem.SelectedMemo = memo;
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
    // リスト先頭のNOTEエントリ選択中は、非表示のTODO選択が誤って動かないよう対象外にする。
    private void MoveChildTodo(int delta)
    {
        if (!IsChildTabSelected)
            return;

        var list = Node.ChildTodoList;
        if (TryGetMoveIndices(list, SelectedChildTodo, delta) is not { } indices)
            return;

        if (list[indices.OldIndex].Status != list[indices.NewIndex].Status)
            return;

        list.Move(indices.OldIndex, indices.NewIndex);
    }

    private bool CanMoveChildTodo(int delta)
    {
        if (!IsChildTabSelected)
            return false;

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
        ChildNode = SelectedChildTodo is { } child ? new TodoNodeViewModel(child, _commandFileService, _completionWatcher) : null;
    }
}
