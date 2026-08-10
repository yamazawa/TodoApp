namespace TodoApp.Services;

/// <summary>
/// ⑤アプリ全体設定のシリアライズ用DTO
///
/// TODO単位ではなく、アプリ全体で1つ保持する。
/// 比率(上下/左右)は境界のドラッグ操作と合わせて別途対応する。
/// </summary>
public sealed record AppSettings(string RootParentDir);
