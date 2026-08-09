using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// NodeSyncTaskをキューで受け取り、バックグラウンドでファイルへ反映するサービス
///
/// フォルダのリネームを使うことで、子孫を含むサブツリーを保って名前変更する。
/// 変更のあったフォルダの直下だけを書き直すので、無関係な深い枝を巻き込まない。
/// </summary>
public class TodoCommandFileService
{
    private readonly Channel<NodeSyncTask> _channel = Channel.CreateUnbounded<NodeSyncTask>();
    private readonly ConditionalWeakTable<TodoItem, string> _lastKnownPath = new();
    private readonly Task _worker;

    public TodoCommandFileService()
    {
        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Enqueue(NodeSyncTask task) => _channel.Writer.TryWrite(task);

    /// <summary>
    /// キューに積まれた書き込みを全て終えるまで待つ
    ///
    /// アプリ終了時に、これ以上Enqueueしないことを確定させてから呼ぶ。
    /// </summary>
    public Task FlushAsync()
    {
        _channel.Writer.TryComplete();
        return _worker;
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var task in _channel.Reader.ReadAllAsync())
        {
            try
            {
                Apply(task);
            }
            catch (IOException)
            {
                // 一時的な入出力エラーは無視する。
                // 対象は変更検知のたびに再度dirty化されるため、次回反映される。
            }
        }
    }

    private void Apply(NodeSyncTask task)
    {
        var parentPath = ResolveParentPath(task);
        var selfPath = MoveOrCreate(task.Identity, parentPath, task.Ordinal, task.Title, task.Status);

        File.WriteAllText(Path.Combine(selfPath, TodoFileNaming.ReadmeFileName), task.Body);
        SyncMemoFiles(selfPath, task.Memos);
        SyncChildFolders(selfPath, task.Children);
    }

    private string ResolveParentPath(NodeSyncTask task)
    {
        if (task.ParentIdentity is null)
        {
            return task.RootParentDir;
        }

        // 親は自分より先に処理されている前提(DrainSyncTasksが深さ順に並べる)。
        return _lastKnownPath.TryGetValue(task.ParentIdentity, out var path) ? path : task.RootParentDir;
    }

    private string MoveOrCreate(TodoItem identity, string parentPath, int ordinal, string? title, Models.Enums.TodoStatus status)
    {
        var desiredName = TodoFileNaming.BuildTodoFolderName(ordinal, title, status);
        var desiredPath = Path.Combine(parentPath, desiredName);

        if (_lastKnownPath.TryGetValue(identity, out var oldPath) && Directory.Exists(oldPath))
        {
            if (oldPath != desiredPath)
            {
                Directory.Move(oldPath, desiredPath);
            }
        }
        else
        {
            Directory.CreateDirectory(desiredPath);
        }

        _lastKnownPath.AddOrUpdate(identity, desiredPath);
        return desiredPath;
    }

    private static void SyncMemoFiles(string selfPath, IReadOnlyList<(string FileName, string Content)> memos)
    {
        var desiredNames = memos.Select(m => m.FileName).ToHashSet();
        foreach (var existing in Directory.GetFiles(selfPath, "*.md"))
        {
            var name = Path.GetFileName(existing);
            if (name != TodoFileNaming.ReadmeFileName && !desiredNames.Contains(name))
            {
                File.Delete(existing);
            }
        }

        foreach (var (fileName, content) in memos)
        {
            File.WriteAllText(Path.Combine(selfPath, fileName), content);
        }
    }

    private void SyncChildFolders(
        string selfPath,
        IReadOnlyList<(TodoItem Identity, int Ordinal, string? Title, Models.Enums.TodoStatus Status)> children)
    {
        var desiredNames = new HashSet<string>();
        foreach (var (identity, ordinal, title, status) in children)
        {
            desiredNames.Add(MoveOrCreate(identity, selfPath, ordinal, title, status)[(selfPath.Length + 1)..]);
        }

        foreach (var existingDir in Directory.GetDirectories(selfPath))
        {
            if (!desiredNames.Contains(Path.GetFileName(existingDir)))
            {
                Directory.Delete(existingDir, recursive: true);
            }
        }
    }
}
