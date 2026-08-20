using System.Diagnostics;

namespace TodoApp.Services;

/// <summary>
/// Claudeセッションを新規PowerShellウィンドウで起動するサービス
///
/// 対象フォルダへ移動した上で、指定セッションを再開しつつ命令文を渡す。
/// </summary>
public static class ClaudeSessionLauncher
{
    /// <summary>
    /// PowerShellを新規ウィンドウで起動し、Claudeセッションを再開する
    /// </summary>
    public static void Launch(string folderPath, string sessionId, string instruction)
    {
        var command =
            $"Set-Location -LiteralPath '{EscapeSingleQuote(folderPath)}'; " +
            $"claude --resume '{EscapeSingleQuote(sessionId)}' '{EscapeSingleQuote(instruction)}'";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add("-NoExit");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        Process.Start(startInfo);
    }

    // PowerShellのシングルクォート文字列内では、'自体を''に二重化してエスケープする。
    private static string EscapeSingleQuote(string value) => value.Replace("'", "''");
}
