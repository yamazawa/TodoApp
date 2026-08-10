namespace TodoApp.ViewModels;

/// <summary>
/// パンくずリストの1項目
///
/// クリック時はParentDirを新しい起動フォルダパスとして親TODOへ遷移する。
/// </summary>
public sealed record BreadcrumbEntry(string Label, string ParentDir);
