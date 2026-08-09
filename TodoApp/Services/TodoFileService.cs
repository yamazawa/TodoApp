using System.IO;
using System.Linq;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// TODOツリーをC.ファイル設計に基づいてファイルへ保存・読込するサービス。
/// これらのファイルは外部から変更されない前提とし、
/// 保存のたびにフォルダの中身を全て書き直す(差分更新はしない)。
/// </summary>
public class TodoFileService
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

    /// <summary>
    /// ルートTODOを保存する。フォルダ名が変わっていればリネームする。
    /// </summary>
    public void SaveRoot(TodoItem root, string parentDir)
    {
        Directory.CreateDirectory(parentDir);

        var desiredName = TodoFileNaming.BuildTodoFolderName(1, root.Title, root.Status);
        var desiredPath = Path.Combine(parentDir, desiredName);

        var existingPath = Directory.GetDirectories(parentDir)
            .FirstOrDefault(dir => TodoFileNaming.ParseTodoFolderName(Path.GetFileName(dir)) is not null);

        if (existingPath is not null && existingPath != desiredPath)
        {
            Directory.Move(existingPath, desiredPath);
        }

        SaveTodoContents(root, desiredPath);
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

    private void SaveTodoContents(TodoItem todo, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        foreach (var entry in Directory.GetFileSystemEntries(folderPath))
        {
            DeleteEntry(entry);
        }

        File.WriteAllText(Path.Combine(folderPath, TodoFileNaming.ReadmeFileName), todo.Body);

        var ordinal = 1;
        foreach (var memo in todo.MemoList)
        {
            var fileName = TodoFileNaming.BuildMemoFileName(ordinal++, memo.Title);
            File.WriteAllText(Path.Combine(folderPath, fileName), memo.Body);
        }

        ordinal = 1;
        foreach (var child in todo.ChildTodoList)
        {
            var childFolderName = TodoFileNaming.BuildTodoFolderName(ordinal++, child.Title, child.Status);
            SaveTodoContents(child, Path.Combine(folderPath, childFolderName));
        }
    }

    private static string ReadReadme(string folderPath)
    {
        var readmePath = Path.Combine(folderPath, TodoFileNaming.ReadmeFileName);
        return File.Exists(readmePath) ? File.ReadAllText(readmePath) : string.Empty;
    }

    private static void DeleteEntry(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        else
        {
            File.Delete(path);
        }
    }

    private static int GetOrdinal(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var underscoreIndex = name.IndexOf('_');
        var ordinalText = underscoreIndex < 0 ? name : name[..underscoreIndex];
        return int.TryParse(ordinalText, out var ordinal) ? ordinal : int.MaxValue;
    }
}
