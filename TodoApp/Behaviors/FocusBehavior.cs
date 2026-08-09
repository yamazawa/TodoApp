using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TodoApp.Behaviors;

/// <summary>
/// バインドした値がtrueになったら要素にフォーカスするビヘイビア
///
/// コードビハインドを使わずにフォーカス制御するための添付プロパティ。
/// </summary>
public static class FocusBehavior
{
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(FocusBehavior),
            new PropertyMetadata(false, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);

    public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || e.NewValue is not true)
        {
            return;
        }

        // バインド直後(コンテナ生成直後)はFocus()が効かないことがあるため、
        // レイアウト確定後にフォーカスする。
        element.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(() =>
            {
                element.Focus();
                Keyboard.Focus(element);
            }));
    }
}
