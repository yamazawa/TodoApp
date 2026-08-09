using System.IO;
using System.Linq;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// C.ファイル設計のフォルダ構成からTODOツリーを読み込むサービス。
/// </summary>
public class TodoFileReader
{
    /// <summary>
    /// 指定フォルダの親フォルダから、ルートTODOフォルダを1つ探して読み込む。
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
            {
                todo.ChildTodoList.Add(LoadTodo(dir));
            }
        }

        foreach (var file in Directory.GetFiles(folderPath, "*.md").OrderBy(GetOrdinal))
        {
            if (Path.GetFileName(file) == TodoFileNaming.ReadmeFileName)
            {
                continue;
            }

            var title = TodoFileNaming.ParseMemoFileName(Path.GetFileNameWithoutExtension(file));
            todo.MemoList.Add(new MemoItem { Title = title, Body = File.ReadAllText(file) });
        }

        return todo;
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
