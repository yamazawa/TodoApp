using System;
using System.Linq;
using TodoApp.Models.Enums;
using TodoApp.Resources;

namespace TodoApp.Services;

/// <summary>
/// TODO/メモ情報のファイル名・フォルダ名の変換ルール
///
/// C.ファイル設計に基づく。
/// </summary>
public static class TodoFileNaming
{
    public const string ReadmeFileName = "0_README.md";
    public const string SaveInfoFileName = "_save.json";
    public const string NullTitlePlaceholder = "NULL";
    private static readonly char[] ForbiddenChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly (TodoStatus Status, Func<string> TextFactory)[] StatusMap =
    [
        (TodoStatus.NotStarted, () => Strings.Status_NotStarted),
        (TodoStatus.InProgress, () => Strings.Status_InProgress),
        (TodoStatus.Done, () => Strings.Status_Done),
    ];

    /// <summary>
    /// TODOフォルダ名を作成する。[連番]_[タイトル]_[ステータス]
    /// </summary>
    public static string BuildTodoFolderName(int ordinal, string? title, TodoStatus status) =>
        $"{ordinal}_{SanitizeTitle(title)}_{StatusToText(status)}";

    /// <summary>
    /// メモ情報ファイル名を作成する。[連番]_[タイトル].md
    /// </summary>
    public static string BuildMemoFileName(int ordinal, string? title) =>
        $"{ordinal}_{SanitizeTitle(title)}.md";

    /// <summary>
    /// TODOフォルダ名からタイトルとステータスを取り出す
    ///
    /// タイトルに「_」が含まれていても解析できるよう、末尾のステータス文字列から先に照合する。
    /// 解析できない場合はnullを返す。
    /// </summary>
    public static (string? Title, TodoStatus Status)? ParseTodoFolderName(string folderName)
    {
        var firstUnderscore = folderName.IndexOf('_');
        if (firstUnderscore < 0)
        {
            return null;
        }

        var afterOrdinal = folderName[(firstUnderscore + 1)..];
        foreach (var (status, textFactory) in StatusMap)
        {
            var suffix = "_" + textFactory();
            if (afterOrdinal.EndsWith(suffix, StringComparison.Ordinal))
            {
                return (ParseTitle(afterOrdinal[..^suffix.Length]), status);
            }
        }

        return null;
    }

    /// <summary>
    /// メモ情報ファイル名(拡張子なし)からタイトルを取り出す
    ///
    /// 解析できない場合はnullを返す。
    /// </summary>
    public static string? ParseMemoFileName(string fileNameWithoutExtension)
    {
        var separatorIndex = fileNameWithoutExtension.IndexOf('_');
        return separatorIndex < 0 ? null : ParseTitle(fileNameWithoutExtension[(separatorIndex + 1)..]);
    }

    private static string SanitizeTitle(string? title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return NullTitlePlaceholder;
        }

        var sanitized = title;
        foreach (var forbidden in ForbiddenChars)
        {
            sanitized = sanitized.Replace(forbidden, '_');
        }

        return sanitized;
    }

    private static string? ParseTitle(string rawTitle) =>
        rawTitle == NullTitlePlaceholder ? null : rawTitle;

    private static string StatusToText(TodoStatus status) =>
        StatusMap.First(m => m.Status == status).TextFactory();
}
