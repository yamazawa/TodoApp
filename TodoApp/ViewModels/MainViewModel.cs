using System.Collections.ObjectModel;
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
/// メイン画面のViewModel。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly TodoCommandFileService _commandFileService;
    private readonly TodoChangeTracker _changeTracker;

    [ObservableProperty]
    private TodoItem _selectedTodo;

    // 選択中タブ/子TODO/メモ情報は表示用情報としてTodoItem側で保持する。
    // ここではSelectedTodoへの委譲プロパティとして公開する。
    public TodoItem? SelectedChildTodo
    {
        get => SelectedTodo.SelectedChildTodo;
        set
        {
            if (ReferenceEquals(SelectedTodo.SelectedChildTodo, value))
            {
                return;
            }

            SelectedTodo.SelectedChildTodo = value;
            OnPropertyChanged();
        }
    }

    public MemoItem? SelectedMemo
    {
        get => SelectedTodo.SelectedMemo;
        set
        {
            if (ReferenceEquals(SelectedTodo.SelectedMemo, value))
            {
                return;
            }

            SelectedTodo.SelectedMemo = value;
            OnPropertyChanged();
        }
    }

    public int SelectedTabIndex
    {
        get => SelectedTodo.SelectedTabIndex;
        set
        {
            if (SelectedTodo.SelectedTabIndex == value)
            {
                return;
            }

            SelectedTodo.SelectedTabIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsChildTabSelected));
            OnPropertyChanged(nameof(IsMemoTabSelected));
        }
    }

    public bool IsChildTabSelected => SelectedTabIndex == 0;

    public bool IsMemoTabSelected => SelectedTabIndex == 1;

    public IReadOnlyList<TodoStatus> StatusOptions { get; } = Enum.GetValues<TodoStatus>();

    public MainViewModel(TodoFileReader fileReader, TodoCommandFileService commandFileService, string rootParentDir)
    {
        _commandFileService = commandFileService;
        _selectedTodo = fileReader.LoadOrCreateRoot(rootParentDir);

        _changeTracker = new TodoChangeTracker(rootParentDir);
        _changeTracker.Attach(_selectedTodo);
    }

    /// <summary>
    /// 変更のあった項目をキューへ積む
    ///
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void EnqueuePendingChanges()
    {
        foreach (var task in _changeTracker.DrainSyncTasks())
        {
            _commandFileService.Enqueue(task);
        }
    }

    // TodoItemツリーの保有者として、購読解除と破棄を行う。
    public void Dispose()
    {
        _changeTracker.Dispose();
        SelectedTodo.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void AddChildTodo()
    {
        var item = new TodoItem { IsEditing = true };
        SelectedTodo.ChildTodoList.Add(item);
        SelectedChildTodo = item;
    }

    [RelayCommand]
    private void DeleteChildTodo() =>
        DeleteWithConfirm(SelectedChildTodo, SelectedTodo.ChildTodoList, Strings.ConfirmDelete_ChildTodoMessage);

    [RelayCommand]
    private void AddMemo()
    {
        var item = new MemoItem { IsEditing = true };
        SelectedTodo.MemoList.Add(item);
        SelectedMemo = item;
    }

    [RelayCommand]
    private void DeleteMemo() =>
        DeleteWithConfirm(SelectedMemo, SelectedTodo.MemoList, Strings.ConfirmDelete_MemoMessage);

    [RelayCommand]
    private void MoveChildTodoUp() => MoveChildTodo(-1);

    [RelayCommand]
    private void MoveChildTodoDown() => MoveChildTodo(1);

    [RelayCommand]
    private void MoveMemoUp() => Move(SelectedTodo.MemoList, SelectedMemo, -1);

    [RelayCommand]
    private void MoveMemoDown() => Move(SelectedTodo.MemoList, SelectedMemo, 1);

    // 子TODOをメモ情報へ変換する。孫以下も再帰的に平坦化して全てメモにする。
    [RelayCommand]
    private void ConvertChildTodoToMemo()
    {
        if (SelectedChildTodo is not { } item)
        {
            return;
        }

        var newMemos = FlattenToMemos(item);
        SelectedTodo.ChildTodoList.Remove(item);
        foreach (var memo in newMemos)
        {
            SelectedTodo.MemoList.Add(memo);
        }

        SelectedTabIndex = 1;
        SelectedMemo = newMemos[0];
    }

    // メモ情報を子TODOへ変換する。
    [RelayCommand]
    private void ConvertMemoToChildTodo()
    {
        if (SelectedMemo is not { } memo)
        {
            return;
        }

        var newItem = new TodoItem { Title = memo.Title, Body = memo.Body };
        SelectedTodo.MemoList.Remove(memo);
        SelectedTodo.ChildTodoList.Add(newItem);

        SelectedTabIndex = 0;
        SelectedChildTodo = newItem;
    }

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
        var list = SelectedTodo.ChildTodoList;
        var item = SelectedChildTodo;
        if (item is null)
        {
            return;
        }

        var oldIndex = list.IndexOf(item);
        var newIndex = oldIndex + delta;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= list.Count)
        {
            return;
        }

        if (list[oldIndex].Status != list[newIndex].Status)
        {
            return;
        }

        list.Move(oldIndex, newIndex);
    }

    private static void Move<T>(ObservableCollection<T> list, T? item, int delta)
    {
        if (item is null)
        {
            return;
        }

        var oldIndex = list.IndexOf(item);
        var newIndex = oldIndex + delta;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= list.Count)
        {
            return;
        }

        list.Move(oldIndex, newIndex);
    }

    private static void DeleteWithConfirm<T>(T? item, ObservableCollection<T> list, string message)
    {
        if (item is null)
        {
            return;
        }

        var result = MessageBox.Show(message, Strings.ConfirmDelete_Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            list.Remove(item);
        }
    }
}
