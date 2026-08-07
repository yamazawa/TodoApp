using System.Windows;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var storageService = new JsonTodoStorageService();
        var viewModel = new MainViewModel(storageService);
        await viewModel.InitializeAsync();

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }
}
