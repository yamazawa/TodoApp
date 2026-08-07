using CommunityToolkit.Mvvm.ComponentModel;

namespace TodoApp.Models;

/// <summary>
/// 1件のTODOを表すモデル。
/// </summary>
public partial class TodoItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;

    public DateTime CreatedAt { get; init; } = DateTime.Now;
}
