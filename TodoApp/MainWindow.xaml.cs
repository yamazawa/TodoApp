using System.Windows;
using System.Windows.Controls;
using TodoApp.Models;

namespace TodoApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: TitledItem item })
        {
            return;
        }

        if (item is TodoItem && ChildTodoListBox.Items.Contains(item))
        {
            ChildTodoListBox.SelectedItem = item;
        }
        else if (item is MemoItem && MemoListBox.Items.Contains(item))
        {
            MemoListBox.SelectedItem = item;
        }

        item.IsEditing = true;
    }

    private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TitledItem item })
        {
            item.IsEditing = false;
        }
    }

    private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox { IsVisible: true } textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
