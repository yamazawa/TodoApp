namespace TodoApp.Services;

/// <summary>
/// ⑤アプリ全体設定のシリアライズ用DTO
///
/// TODO単位ではなく、アプリ全体で1つ保持する。
/// BottomLeftRightRatioは境界のドラッグ操作で変更でき、変更のたびに保存する。
/// 再帰の深さに関わらず、全階層で1つの値を共有する。
/// </summary>
public sealed record AppSettings(
    string RootParentDir,
    double BottomLeftRightRatio = 0.2,
    double WindowWidth = Styles.LayoutConstants.WindowWidth,
    double WindowHeight = Styles.LayoutConstants.WindowHeight);
