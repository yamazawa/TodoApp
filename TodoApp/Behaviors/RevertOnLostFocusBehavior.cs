using System.Windows;
using System.Windows.Controls;

namespace TodoApp.Behaviors;

/// <summary>
/// フォーカスアウト時、TextBox.Textをバインド元の値に再同期するビヘイビア
///
/// タイトルは空文字を拒否してバインド元の値を変えないため、
/// 空のまま確定しようとした場合は表示だけが元の値に戻る。
/// </summary>
public static class RevertOnLostFocusBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(RevertOnLostFocusBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if (e.NewValue is true)
            textBox.LostFocus += OnLostFocus;
        else
            textBox.LostFocus -= OnLostFocus;
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
    }
}
