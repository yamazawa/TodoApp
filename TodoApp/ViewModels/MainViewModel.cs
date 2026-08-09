using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Models.Enums;

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
    private void AddChildTodo() => SelectedTodo.ChildTodoList.Add(new TodoItem());

    [RelayCommand]
    private void DeleteChildTodo(TodoItem? item) =>
        DeleteWithConfirm(item, SelectedTodo.ChildTodoList, "この子TODOを削除しますか？\n(子要素も全て削除されます)");

    [RelayCommand]
    private void AddMemo() => SelectedTodo.MemoList.Add(new MemoItem());

    [RelayCommand]
    private void DeleteMemo(MemoItem? item) =>
        DeleteWithConfirm(item, SelectedTodo.MemoList, "このメモ情報を削除しますか？");

    private static void DeleteWithConfirm<T>(T? item, ObservableCollection<T> list, string message)
    {
        if (item is null)
        {
            return;
        }

        var result = MessageBox.Show(message, "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            list.Remove(item);
        }
    }
}
