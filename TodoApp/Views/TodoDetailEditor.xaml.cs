using System.Windows;
using System.Windows.Controls;

namespace TodoApp.Views;

/// <summary>
/// メモ情報の本文を編集する共通コントロール
///
/// タイトルはリスト側の右クリック「編集」で変更する(パンくずリストに表示するため、
/// ここでは重複するタイトル表示を持たない)。
/// </summary>
public partial class TodoDetailEditor : UserControl
{
    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(string), typeof(TodoDetailEditor),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public TodoDetailEditor()
    {
        InitializeComponent();
    }

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }
}
