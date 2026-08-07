using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// TODOリストの永続化を担うサービスのインターフェース。
/// </summary>
public interface ITodoStorageService
{
    Task<List<TodoItem>> LoadAsync();

    Task SaveAsync(IEnumerable<TodoItem> items);
}
