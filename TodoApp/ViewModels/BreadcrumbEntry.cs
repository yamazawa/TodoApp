using TodoApp.Models;

namespace TodoApp.ViewModels;

/// <summary>
/// パンくずリストの1項目
///
/// 現在ロード中のツリー内の祖先・再帰的に表示中のTODO項目はTargetTodo(インメモリ切替)で、
/// ツリーの外側(ファイル構成上のさらに親)はParentDir(再読込)で遷移する。
/// 現在表示中のTODO自身はどちらも設定せず、リンクにしない。
/// </summary>
public sealed record BreadcrumbEntry(string Label, TodoItem? TargetTodo, string? ParentDir)
{
    public bool IsLink => TargetTodo is not null || ParentDir is not null;

    public bool IsCurrent => !IsLink;
}
