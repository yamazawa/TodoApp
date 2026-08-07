using System.IO;
using System.Text.Json;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// %AppData%\TodoApp\todos.json にTODOリストを保存・読込するサービス。
/// </summary>
public class JsonTodoStorageService : ITodoStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public JsonTodoStorageService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoApp");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "todos.json");
    }

    public async Task<List<TodoItem>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var items = await JsonSerializer.DeserializeAsync<List<TodoItem>>(stream, SerializerOptions);
            return items ?? [];
        }
        catch (JsonException)
        {
            // 破損したファイルは無視して空リストから始める
            return [];
        }
    }

    public async Task SaveAsync(IEnumerable<TodoItem> items)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, items.ToList(), SerializerOptions);
    }
}
