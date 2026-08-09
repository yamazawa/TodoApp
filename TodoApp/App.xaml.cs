using System.IO;
using System.Windows;
using System.Windows.Threading;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // ⑤アプリ全体設定(起動時フォルダパス)はタスク4で実装するため、
    // タスク3では固定の既定フォルダを使う。
    private static readonly string DefaultRootParentDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TodoApp");

    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(5);

    private MainViewModel? _viewModel;
    private DispatcherTimer? _autoSaveTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var fileService = new TodoFileService();
        _viewModel = new MainViewModel(fileService, DefaultRootParentDir);

        _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveInterval };
        _autoSaveTimer.Tick += (_, _) => _viewModel.Save();
        _autoSaveTimer.Start();

        var mainWindow = new MainWindow { DataContext = _viewModel };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 終了時は同期的に保存し、直前の変更を確実に残す。
        _viewModel?.Save();
        base.OnExit(e);
    }
}
