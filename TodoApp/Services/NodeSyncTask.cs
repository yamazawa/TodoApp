using TodoApp.Models;
using TodoApp.Models.Enums;

namespace TodoApp.Services;

/// <summary>
/// TodoItem 1件分の同期指示(スナップショット)。
/// UIスレッドで作成し、バックグラウンドの書き込みスレッドへ渡す。
/// 生きているモデルオブジェクトへの参照は識別用のキーとしてのみ使い、
/// バックグラウンド側でプロパティを読み直すことはしない。
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
