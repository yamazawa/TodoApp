using System.IO;
using System.Linq;
using System.Text.Json;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// C.ファイル設計のフォルダ構成からTODOツリーを読み込むサービス。
/// </summary>
public class TodoFileReader
{
    /// <summary>
    /// 指定フォルダの親フォルダから、ルートTODOフォルダを1つ探して読み込む
    ///
    /// 見つからない場合は新規のTODOを作成する。
    /// </summary>
    public TodoItem LoadOrCreateRoot(string parentDir)
    {
        Directory.CreateDirectory(parentDir);

        var rootFolder = Directory.GetDirectories(parentDir)
            .FirstOrDefault(dir => TodoFileNaming.ParseTodoFolderName(Path.GetFileName(dir)) is not null);

        return rootFolder is null ? new TodoItem() : LoadTodo(rootFolder);
    }

    private TodoItem LoadTodo(string folderPath)
    {
        var parsed = TodoFileNaming.ParseTodoFolderName(Path.GetFileName(folderPath));
        var todo = new TodoItem
        {
            Title = parsed?.Title,
            Status = parsed?.Status ?? Models.Enums.TodoStatus.NotStarted,
            Body = ReadReadme(folderPath),
        };

        foreach (var dir in Directory.GetDirectories(folderPath).OrderBy(GetOrdinal))
        {
            if (TodoFileNaming.ParseTodoFolderName(Path.GetFileName(dir)) is not null)
                todo.ChildTodoList.Add(LoadTodo(dir));
        }

        foreach (var file in Directory.GetFiles(folderPath, "*.md").OrderBy(GetOrdinal))
        {
            if (Path.GetFileName(file) == TodoFileNaming.ReadmeFileName)
                continue;

            var title = TodoFileNaming.ParseMemoFileName(Path.GetFileNameWithoutExtension(file));
            todo.MemoList.Add(new MemoItem { Title = title, Body = File.ReadAllText(file) });
        }

        ApplySaveInfo(todo, folderPath);
        return todo;
    }

    // ④保存情報(表示用情報)を読み込む。ファイルが無い/壊れている場合は既定値のまま。
    private static void ApplySaveInfo(TodoItem todo, string folderPath)
    {
        var saveInfoPath = Path.Combine(folderPath, TodoFileNaming.SaveInfoFileName);
        if (!File.Exists(saveInfoPath))
            return;

        try
        {
            var json = File.ReadAllText(saveInfoPath);
            var info = JsonSerializer.Deserialize<TodoSaveInfo>(json);
            if (info is null)
                return;

            todo.SelectedTabIndex = info.SelectedTabIndex;
            if (info.SelectedChildIndex is { } childIndex && childIndex >= 0 && childIndex < todo.ChildTodoList.Count)
                todo.SelectedChildTodo = todo.ChildTodoList[childIndex];

            if (info.SelectedMemoIndex is { } memoIndex && memoIndex >= 0 && memoIndex < todo.MemoList.Count)
                todo.SelectedMemo = todo.MemoList[memoIndex];
        }
        catch (JsonException)
        {
            // 壊れた保存情報は無視する。
        }
    }

    private static string ReadReadme(string folderPath)
    {
        var readmePath = Path.Combine(folderPath, TodoFileNaming.ReadmeFileName);
        return File.Exists(readmePath) ? File.ReadAllText(readmePath) : string.Empty;
    }

    private static int GetOrdinal(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var underscoreIndex = name.IndexOf('_');
        var ordinalText = underscoreIndex < 0 ? name : name[..underscoreIndex];
        return int.TryParse(ordinalText, out var ordinal) ? ordinal : int.MaxValue;
    }
}
