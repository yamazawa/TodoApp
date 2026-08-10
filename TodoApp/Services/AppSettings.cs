namespace TodoApp.Services;

/// <summary>
/// ⑤アプリ全体設定のシリアライズ用DTO
///
/// TODO単位ではなく、アプリ全体で1つ保持する。
/// 比率は各境界のドラッグ操作で変更でき、変更のたびに保存する。
/// NestedXxxRatioは下半分の右側(子TODO項目表示時)の内部比率。
/// </summary>
public sealed record AppSettings(
    string RootParentDir,
    double TopBottomRatio = 0.5,
    double BottomLeftRightRatio = 0.2,
    double NestedTopBottomRatio = 0.5,
    double NestedLeftRightRatio = 0.2,
    double WindowWidth = Styles.LayoutConstants.WindowWidth,
    double WindowHeight = Styles.LayoutConstants.WindowHeight);
