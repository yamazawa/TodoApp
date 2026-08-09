using TodoApp.Models;
using TodoApp.Models.Enums;

namespace TodoApp.Services;

/// <summary>
/// TodoItem 1件分の同期指示(スナップショット)
///
/// UIスレッドで作成し、バックグラウンドの書き込みスレッドへ渡す。
/// モデルの参照は識別キーとしてのみ使い、プロパティは読み直さない。
/// </summary>
public sealed record NodeSyncTask(
    TodoItem Identity,
    TodoItem? ParentIdentity,
    string RootParentDir,
    int Ordinal,
    string? Title,
    TodoStatus Status,
    string Body,
    IReadOnlyList<(string FileName, string Content)> Memos,
    IReadOnlyList<(TodoItem Identity, int Ordinal, string? Title, TodoStatus Status)> Children);
