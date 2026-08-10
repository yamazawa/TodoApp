using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Resources;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// メイン画面のViewModel。
///
/// ルートTODOに対する子TODO/メモ情報の操作はRootNode(TodoNodeViewModel)に委譲する。
/// 下半分の右側に子TODOの孫項目パネルを表示するため、選択中の子TODOをChildNodeとして持つ。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly TodoFileReader _fileReader;
    private readonly TodoCommandFileService _commandFileService;
    private readonly AppSettingsService _settingsService;
    private TodoChangeTracker _changeTracker;
    private TodoItem _loadedRoot;
    private string _rootParentDir;
    private bool _settingsDirty;

    // 画面に表示中のTODO。ロード済みツリー内の任意のノードを指せる(移動リンクで切替可能)。
    [ObservableProperty]
    private TodoItem _selectedTodo;

    public TodoNodeViewModel RootNode { get; private set; }

    [ObservableProperty]
    private TodoNodeViewModel? _childNode;

    // 親TODOへのリンク一覧。遠い祖先→近い親の順に並ぶ。
    [ObservableProperty]
    private IReadOnlyList<BreadcrumbEntry> _breadcrumbs = [];

    // 境界のドラッグ操作で変更できる表示比率。⑤アプリ全体設定として保存する。
    [ObservableProperty]
    private double _topBottomRatio;

    [ObservableProperty]
    private double _bottomLeftRightRatio;

    // 下半分の右側(子TODO項目表示時)の内部比率。親TODOの表示割合とは別に保持する。
    [ObservableProperty]
    private double _nestedTopBottomRatio;

    [ObservableProperty]
    private double _nestedLeftRightRatio;

    // ウィンドウサイズ。⑤アプリ全体設定として保存する。
    [ObservableProperty]
    private double _windowWidth;

    [ObservableProperty]
    private double _windowHeight;

    public MainViewModel(TodoFileReader fileReader, TodoCommandFileService commandFileService, AppSettingsService settingsService, AppSettings settings)
    {
        _fileReader = fileReader;
        _commandFileService = commandFileService;
        _settingsService = settingsService;
        _rootParentDir = settings.RootParentDir;
        _loadedRoot = fileReader.LoadOrCreateRoot(_rootParentDir);
        _selectedTodo = _loadedRoot;

        _changeTracker = new TodoChangeTracker(_rootParentDir);
        _changeTracker.Attach(_loadedRoot);

        RootNode = new TodoNodeViewModel(_selectedTodo);
        RootNode.PropertyChanged += OnRootNodePropertyChanged;
        RebuildChildNode();
        RebuildBreadcrumbs();

        _topBottomRatio = settings.TopBottomRatio;
        _bottomLeftRightRatio = settings.BottomLeftRightRatio;
        _nestedTopBottomRatio = settings.NestedTopBottomRatio;
        _nestedLeftRightRatio = settings.NestedLeftRightRatio;
        _windowWidth = settings.WindowWidth;
        _windowHeight = settings.WindowHeight;
    }

    partial void OnTopBottomRatioChanged(double value) => MarkSettingsDirty();

    partial void OnBottomLeftRightRatioChanged(double value) => MarkSettingsDirty();

    partial void OnNestedTopBottomRatioChanged(double value) => MarkSettingsDirty();

    partial void OnNestedLeftRightRatioChanged(double value) => MarkSettingsDirty();

    partial void OnWindowWidthChanged(double value) => MarkSettingsDirty();

    partial void OnWindowHeightChanged(double value) => MarkSettingsDirty();

    private void MarkSettingsDirty() => _settingsDirty = true;

    /// <summary>
    /// 比率・ウィンドウサイズに変更があれば⑤アプリ全体設定を保存する
    ///
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void SaveSettingsIfDirty()
    {
        if (!_settingsDirty)
            return;

        _settingsDirty = false;
        _settingsService.Save(new AppSettings(
            _rootParentDir, TopBottomRatio, BottomLeftRightRatio, NestedTopBottomRatio, NestedLeftRightRatio, WindowWidth, WindowHeight));
    }

    /// <summary>
    /// パンくずリンクをクリックしたときの遷移
    ///
    /// ロード済みツリー内の祖先(TargetTodo)はインメモリで切り替え、
    /// ツリーの外側(ParentDir)はディスクから読み直す。
    /// </summary>
    [RelayCommand]
    private void NavigateToBreadcrumb(BreadcrumbEntry entry)
    {
        if (entry.TargetTodo is { } target)
        {
            SelectedTodo = target;
            return;
        }

        if (entry.ParentDir is { } parentDir)
            ReloadFromParentDir(parentDir);
    }

    /// <summary>
    /// 子TODO/孫TODOの「移動」リンクから、そのTODOを現在の表示位置へ切り替える
    ///
    /// ロード済みツリー内のノードを直接指すだけなので、ディスクの再読込は行わない。
    /// </summary>
    [RelayCommand]
    private void NavigateToChild(TodoItem target) => SelectedTodo = target;

    // ファイルシステムから読み直し、ロード済みツリーを丸ごと差し替える。
    private void ReloadFromParentDir(string parentDir)
    {
        EnqueuePendingChanges();

        var oldRoot = _loadedRoot;
        _changeTracker.Dispose();

        _rootParentDir = parentDir;
        _loadedRoot = _fileReader.LoadOrCreateRoot(_rootParentDir);
        _changeTracker = new TodoChangeTracker(_rootParentDir);
        _changeTracker.Attach(_loadedRoot);

        SelectedTodo = _loadedRoot;
        oldRoot.Dispose();
        MarkSettingsDirty();
    }

    // SelectedTodoの差し替えに合わせてRootNode/パンくずリストも作り直す。
    partial void OnSelectedTodoChanged(TodoItem? oldValue, TodoItem newValue)
    {
        RootNode.PropertyChanged -= OnRootNodePropertyChanged;
        RootNode.Dispose();
        RootNode = new TodoNodeViewModel(newValue);
        RootNode.PropertyChanged += OnRootNodePropertyChanged;
        OnPropertyChanged(nameof(RootNode));
        RebuildChildNode();
        RebuildBreadcrumbs();
    }

    // 下半分の右側(子TODO選択時)の孫項目パネル用に、選択中の子TODOをラップし直す。
    private void OnRootNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoNodeViewModel.SelectedChildTodo))
            RebuildChildNode();
    }

    private void RebuildChildNode()
    {
        ChildNode?.Dispose();
        ChildNode = RootNode.SelectedChildTodo is { } child ? new TodoNodeViewModel(child) : null;
    }

    // パンくずリストを組み立てる。
    // ①ファイルシステム上の祖先(ロード済みツリーの外側、フォルダ名の規則+README存在で判定)。
    // ②インメモリの祖先(ロード済みツリー内、移動リンクで下りてきた分)。
    // ファイルの中身(本文)は見ない。
    private void RebuildBreadcrumbs()
    {
        var entries = new List<BreadcrumbEntry>();
        var candidate = _rootParentDir;

        while (TodoFileNaming.ParseTodoFolderName(Path.GetFileName(candidate)) is { } parsed &&
               File.Exists(Path.Combine(candidate, TodoFileNaming.ReadmeFileName)))
        {
            var parentDir = Path.GetDirectoryName(candidate);
            if (parentDir is null)
                break;

            entries.Add(new BreadcrumbEntry(parsed.Title ?? Strings.Breadcrumb_NullTitle, TargetTodo: null, parentDir));
            candidate = parentDir;
        }

        entries.Reverse();

        var inMemoryAncestors = new List<TodoItem>();
        for (var current = _changeTracker.GetParent(SelectedTodo); current is not null; current = _changeTracker.GetParent(current))
        {
            inMemoryAncestors.Add(current);
        }

        inMemoryAncestors.Reverse();
        entries.AddRange(inMemoryAncestors.Select(t => new BreadcrumbEntry(t.DisplayTitle, t, ParentDir: null)));

        Breadcrumbs = entries;
    }

    /// <summary>
    /// 変更のあった項目をキューへ積む
    ///
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void EnqueuePendingChanges()
    {
        foreach (var task in _changeTracker.DrainSyncTasks())
        {
            _commandFileService.Enqueue(task);
        }
    }

    // TodoItemツリーの保有者として、購読解除と破棄を行う。
    // SelectedTodoは移動リンクでツリーの一部を指しているだけの場合があるため、
    // 破棄は必ずツリー全体の保有者であるLoadedRootに対して行う。
    public void Dispose()
    {
        ChildNode?.Dispose();
        RootNode.PropertyChanged -= OnRootNodePropertyChanged;
        RootNode.Dispose();
        _changeTracker.Dispose();
        _loadedRoot.Dispose();
        GC.SuppressFinalize(this);
    }
}
