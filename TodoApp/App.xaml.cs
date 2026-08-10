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
    // ⑤アプリ全体設定に起動フォルダパスが無い場合の既定値。
    private static readonly string DefaultRootParentDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TodoApp");

    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(1);

    private MainViewModel? _viewModel;
    private TodoCommandFileService? _commandFileService;
    private AppSettingsService? _settingsService;
    private DispatcherTimer? _autoSaveTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new AppSettingsService();
        var appSettings = _settingsService.LoadOrCreateDefault(DefaultRootParentDir);

        var fileReader = new TodoFileReader();
        _commandFileService = new TodoCommandFileService();
        _viewModel = new MainViewModel(fileReader, _commandFileService, _settingsService, appSettings);

        _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveInterval };
        _autoSaveTimer.Tick += (_, _) =>
        {
            _viewModel.EnqueuePendingChanges();
            _viewModel.SaveSettingsIfDirty();
        };
        _autoSaveTimer.Start();

        var mainWindow = new MainWindow { DataContext = _viewModel };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 終了時は、残っている変更を全てキューへ積んでから
        // 書き込みが終わるまで同期的に待つ。
        _viewModel?.EnqueuePendingChanges();
        _viewModel?.SaveSettingsIfDirty();
        _commandFileService?.FlushAsync().GetAwaiter().GetResult();
        _viewModel?.Dispose();
        base.OnExit(e);
    }
}
