using System.Windows;
using System.Windows.Controls;
using TodoApp.Models.Enums;

namespace TodoApp.Views;

/// <summary>
/// タイトル/ステータス/本文を編集する共通コントロール
///
/// 上半分、下半分の右側(子TODO/メモ情報の詳細)で使い回す。
/// メモ情報にはステータスがないため、ShowStatusで表示を切り替える。
/// </summary>
public partial class TodoDetailEditor : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(TodoDetailEditor),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    // タイトルがnullの間、フォーカスが無いときに代わりに表示する文字列(本文の1行目)。
    public static readonly DependencyProperty DisplayTitleProperty = DependencyProperty.Register(
        nameof(DisplayTitle), typeof(string), typeof(TodoDetailEditor), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(string), typeof(TodoDetailEditor),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(TodoStatus), typeof(TodoDetailEditor),
        new FrameworkPropertyMetadata(TodoStatus.NotStarted, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StatusOptionsProperty = DependencyProperty.Register(
        nameof(StatusOptions), typeof(IEnumerable<TodoStatus>), typeof(TodoDetailEditor));

    public static readonly DependencyProperty ShowStatusProperty = DependencyProperty.Register(
        nameof(ShowStatus), typeof(bool), typeof(TodoDetailEditor), new PropertyMetadata(true));

    // 孫項目詳細(TODO項目)でのみ使う、子TODOの完了数/総数。nullなら非表示。
    public static readonly DependencyProperty ChildCountTextProperty = DependencyProperty.Register(
        nameof(ChildCountText), typeof(string), typeof(TodoDetailEditor), new PropertyMetadata(null));

    public TodoDetailEditor()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string DisplayTitle
    {
        get => (string)GetValue(DisplayTitleProperty);
        set => SetValue(DisplayTitleProperty, value);
    }

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public TodoStatus Status
    {
        get => (TodoStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public IEnumerable<TodoStatus>? StatusOptions
    {
        get => (IEnumerable<TodoStatus>?)GetValue(StatusOptionsProperty);
        set => SetValue(StatusOptionsProperty, value);
    }

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }

    public string? ChildCountText
    {
        get => (string?)GetValue(ChildCountTextProperty);
        set => SetValue(ChildCountTextProperty, value);
    }
}
