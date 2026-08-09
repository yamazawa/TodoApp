using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Models.Enums;
using TodoApp.Resources;

namespace TodoApp.ViewModels;

/// <summary>
/// メイン画面のViewModel。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private TodoItem _selectedTodo = new();

    public IReadOnlyList<TodoStatus> StatusOptions { get; } = Enum.GetValues<TodoStatus>();

    [RelayCommand]
    private void AddChildTodo() => SelectedTodo.ChildTodoList.Add(new TodoItem { IsEditing = true });

    [RelayCommand]
    private void DeleteChildTodo(TodoItem? item) =>
        DeleteWithConfirm(item, SelectedTodo.ChildTodoList, Strings.ConfirmDelete_ChildTodoMessage);

    [RelayCommand]
    private void AddMemo() => SelectedTodo.MemoList.Add(new MemoItem { IsEditing = true });

    [RelayCommand]
    private void DeleteMemo(MemoItem? item) =>
        DeleteWithConfirm(item, SelectedTodo.MemoList, Strings.ConfirmDelete_MemoMessage);

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
