using System.IO;
using System.Linq;
using System.Text.Json;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// TODOツリーをC.ファイル設計のフォルダ構成へ丸ごと書き出すサービス
///
/// 保存のたびにフォルダの中身を全て書き直す(差分更新はしない)。
/// 巨大な木では時間が掛かりうるため、Export用途として使う。
/// 通常の自動保存はTodoCommandFileService(差分更新)を使う。
/// </summary>
public class TodoFileWriter
{
    /// <summary>
    /// ルートTODOを保存する
    ///
    /// フォルダ名が変わっていればリネームする。
    /// </summary>
    public void SaveRoot(TodoItem root, string parentDir)
    {
        Directory.CreateDirectory(parentDir);

        var desiredName = TodoFileNaming.BuildTodoFolderName(1, root.Title, root.Status);
        var desiredPath = Path.Combine(parentDir, desiredName);

        var existingPath = Directory.GetDirectories(parentDir)
            .FirstOrDefault(dir => TodoFileNaming.ParseTodoFolderName(Path.GetFileName(dir)) is not null);

        if (existingPath is not null && existingPath != desiredPath)
            Directory.Move(existingPath, desiredPath);

        SaveTodoContents(root, desiredPath);
    }

    private void SaveTodoContents(TodoItem todo, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        foreach (var entry in Directory.GetFileSystemEntries(folderPath))
        {
            DeleteEntry(entry);
        }

        File.WriteAllText(Path.Combine(folderPath, TodoFileNaming.ReadmeFileName), todo.Body);
        WriteSaveInfo(todo, folderPath);

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

    // ④保存情報(表示用情報)を書き出す。選択中の子/メモはインデックスで保存する。
    private static void WriteSaveInfo(TodoItem todo, string folderPath)
    {
        var info = new TodoSaveInfo(
            todo.SelectedTabIndex,
            todo.SelectedChildTodo is null ? null : todo.ChildTodoList.IndexOf(todo.SelectedChildTodo),
            todo.SelectedMemo is null ? null : todo.MemoList.IndexOf(todo.SelectedMemo));
        var json = JsonSerializer.Serialize(info);
        File.WriteAllText(Path.Combine(folderPath, TodoFileNaming.SaveInfoFileName), json);
    }

    private static void DeleteEntry(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        else
            File.Delete(path);
    }
}
