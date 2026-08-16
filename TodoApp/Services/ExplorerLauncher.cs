using System.Diagnostics;
using System.IO;

namespace TodoApp.Services;

/// <summary>
/// エクスプローラーでフォルダ・ファイルを開くサービス
/// </summary>
public static class ExplorerLauncher
{
    /// <summary>
    /// フォルダをエクスプローラーで開く
    /// </summary>
    public static void OpenFolder(string? path)
    {
        if (path is null || !Directory.Exists(path))
            return;

        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    /// <summary>
    /// ファイルを選択状態でエクスプローラーを開く
    /// </summary>
    public static void SelectFile(string? path)
    {
        if (path is null || !File.Exists(path))
            return;

        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{path}\"" });
    }
}
