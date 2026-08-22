using System.IO;
using TodoApp.Models;
using TodoApp.Models.Enums;

namespace TodoApp.Services;

/// <summary>
/// Claudeセッションの完了合図ファイルをポーリング検知するサービス
///
/// 監視対象はTodoItem単位で保持し、完了合図ファイルを見つけたら
/// 削除してステータスを完了へ変更する。
/// </summary>
public class ClaudeCompletionWatcher
{
    // Claude側に作成を指示する完了合図ファイル名。
    public const string MarkerFileName = ".claude_done";

    private readonly List<(TodoItem Node, string MarkerPath)> _pending = [];

    /// <summary>
    /// 完了合図の監視対象に追加する
    ///
    /// 同じNodeを既に監視中の場合は、監視先パスを最新のものへ差し替える。
    /// </summary>
    public void Watch(TodoItem node, string folderPath)
    {
        _pending.RemoveAll(entry => entry.Node == node);
        _pending.Add((node, Path.Combine(folderPath, MarkerFileName)));
    }

    /// <summary>
    /// 監視対象を1回分チェックし、完了合図があればステータスを完了へ変更する
    /// </summary>
    public void PollOnce()
    {
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var (node, markerPath) = _pending[i];
            if (!File.Exists(markerPath))
                continue;

            File.Delete(markerPath);
            node.Status = TodoStatus.Done;
            _pending.RemoveAt(i);
        }
    }
}
