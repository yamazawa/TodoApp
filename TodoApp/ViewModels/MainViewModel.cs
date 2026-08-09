using System.Collections.ObjectModel;
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
public partial class MainViewModel : ObservableObject
{
    private readonly TodoFileService _fileService;
    private readonly string _rootParentDir;

    [ObservableProperty]
    private TodoItem _selectedTodo;

    [ObservableProperty]
    private TodoItem? _selectedChildTodo;

    [ObservableProperty]
    private MemoItem? _selectedMemo;

    public IReadOnlyList<TodoStatus> StatusOptions { get; } = Enum.GetValues<TodoStatus>();

    public MainViewModel(TodoFileService fileService, string rootParentDir)
    {
        _fileService = fileService;
        _rootParentDir = rootParentDir;
        _selectedTodo = fileService.LoadOrCreateRoot(rootParentDir);
    }

    /// <summary>
    /// 現在の内容をファイルへ保存する。
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void Save() => _fileService.SaveRoot(SelectedTodo, _rootParentDir);

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
