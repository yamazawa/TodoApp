using System.IO;
using System.Text.Json;

namespace TodoApp.Services;

/// <summary>
/// ⑤アプリ全体設定を読み書きするサービス
///
/// %AppData%\TodoApp(_Debug)\settings.json に保存する。
/// TODOツリー本体とは別のフォルダで管理する。
/// </summary>
public class AppSettingsService
{
    private readonly string _settingsPath;

    public AppSettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataPaths.FolderName);
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    /// <summary>
    /// 設定を読み込む
    ///
    /// ファイルが無い/壊れている場合は既定値で新規作成して保存する。
    /// </summary>
    public AppSettings LoadOrCreateDefault(string defaultRootParentDir)
    {
        var loaded = TryLoad();
        if (loaded is not null)
            return loaded;

        var defaultSettings = new AppSettings(defaultRootParentDir);
        Save(defaultSettings);
        return defaultSettings;
    }

    public void Save(AppSettings settings)
    {
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));
    }

    private AppSettings? TryLoad()
    {
        if (!File.Exists(_settingsPath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
