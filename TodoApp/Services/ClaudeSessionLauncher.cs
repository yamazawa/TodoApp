using System.Diagnostics;

namespace TodoApp.Services;

/// <summary>
/// Claudeセッションを新規PowerShellウィンドウで起動するサービス
///
/// 対象フォルダへ移動した上で、新規発行または再開したセッションへ命令文を渡す。
/// ClaudeCompletionWatcherが完了を検知できるよう、命令文の末尾に
/// 完了合図ファイル作成の指示を自動で追記する。
/// </summary>
public static class ClaudeSessionLauncher
{
    /// <summary>
    /// PowerShellを新規ウィンドウで起動し、Claudeセッションを開始/再開する
    /// </summary>
    public static void Launch(string folderPath, string sessionId, string instruction, bool isNewSession)
    {
        var fullInstruction = instruction + BuildCompletionMarkerSuffix();
        var sessionOption = isNewSession ? "--session-id" : "--resume";

        // 実際に渡している値をウィンドウ上で確認できるよう、実行前に表示する。
        var command =
            $"Set-Location -LiteralPath '{EscapeSingleQuote(folderPath)}'; " +
            $"Write-Host 'SessionId ({(isNewSession ? "new" : "resume")}): {EscapeSingleQuote(sessionId)}'; " +
            $"Write-Host 'Instruction: {EscapeSingleQuote(instruction)}'; " +
            $"claude {sessionOption} '{EscapeSingleQuote(sessionId)}' '{EscapeSingleQuote(fullInstruction)}'";

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

    private static string BuildCompletionMarkerSuffix() =>
        $"\n\n(作業が完了したら、フォルダ直下に「{ClaudeCompletionWatcher.MarkerFileName}」という空ファイルを作成してください)";

    // PowerShellのシングルクォート文字列内では、'自体を''に二重化してエスケープする。
    private static string EscapeSingleQuote(string value) => value.Replace("'", "''");
}
