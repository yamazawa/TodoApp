using TodoApp.Models;

namespace TodoApp.ViewModels;

/// <summary>
/// パンくずリストの1項目
///
/// 現在ロード中のツリー内の祖先はTargetTodo(インメモリ切替)で、
/// ツリーの外側(ファイル構成上のさらに親)はParentDir(再読込)で遷移する。
/// どちらか一方のみが設定される。
/// </summary>
public sealed record BreadcrumbEntry(string Label, TodoItem? TargetTodo, string? ParentDir);
