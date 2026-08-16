using System.Windows;

namespace TodoApp.Styles;

/// <summary>
/// View全体で使うレイアウト関連の定数値
///
/// サイズ・余白を変更する際はここを直す。
/// </summary>
public static class LayoutConstants
{
    public const double WindowHeight = 600;
    public const double WindowWidth = 480;
    public const double WindowMinHeight = 400;
    public const double WindowMinWidth = 360;

    public static readonly Thickness RootMargin = new(12);
    public static readonly Thickness SectionSpacing = new(0, 0, 0, 8);
    public static readonly Thickness FieldSpacing = new(0, 0, 0, 4);
    public static readonly Thickness ControlPadding = new(4);
    public static readonly Thickness TabContentMargin = new(4);
    public static readonly Thickness ButtonSpacing = new(0, 0, 8, 0);

    public static readonly GridLength SplitterThickness = new(4);

    // ドラッグ操作を終えた位置が、null(自動サイズ)時の横幅とこの範囲内なら
    // nullに丸めて、以後も内容に合わせた自動サイズを保つ。
    public const double ListWidthSnapTolerance = 5;
}
