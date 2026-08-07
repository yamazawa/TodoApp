using System.Windows;
using System.Windows.Input;
using TodoApp.ViewModels;

namespace TodoApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void NewTodoTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel viewModel && viewModel.AddCommand.CanExecute(null))
        {
            viewModel.AddCommand.Execute(null);
        }
    }
}
