namespace TodoApp.Services;

/// <summary>
/// ⑤アプリ全体設定のシリアライズ用DTO
///
/// TODO単位ではなく、アプリ全体で1つ保持する。
/// リストの表示横幅はTODO単位の④保存情報(TodoSaveInfo.ListWidth)で保持する。
/// </summary>
public sealed record AppSettings(
    string RootParentDir,
    double WindowWidth = Styles.LayoutConstants.WindowWidth,
    double WindowHeight = Styles.LayoutConstants.WindowHeight);
