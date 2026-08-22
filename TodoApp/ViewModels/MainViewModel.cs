using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// メイン画面のViewModel。
///
/// ルートTODOに対する子TODO/メモ情報の操作、および子TODOの再帰表示は
/// RootNode(TodoNodeViewModel)に委譲する。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly TodoFileReader _fileReader;
    private readonly TodoCommandFileService _commandFileService;
    private readonly ClaudeCompletionWatcher _completionWatcher;
    private readonly AppSettingsService _settingsService;
    private TodoChangeTracker _changeTracker;
    private TodoItem _loadedRoot;
    private string _rootParentDir;
    private bool _settingsDirty;

    // パンくずリストの「再帰的に表示中のTODO項目」の算出のために購読中のTODO。
    // 表示位置・タブ・子TODOの変化を検知してパンくずリストを再構築する。
    private readonly List<TodoItem> _displayedChain = [];

    // パンくずリスト末尾に表示する、現在表示中のメモ情報。
    private MemoItem? _displayedMemo;

    // 画面に表示中のTODO。ロード済みツリー内の任意のノードを指せる(移動リンクで切替可能)。
    [ObservableProperty]
    private TodoItem _selectedTodo;

    public TodoNodeViewModel RootNode { get; private set; }

    // 親TODOへのリンク一覧。遠い祖先→近い親の順に並ぶ。
    [ObservableProperty]
    private IReadOnlyList<BreadcrumbEntry> _breadcrumbs = [];

    // ウィンドウサイズ。⑤アプリ全体設定として保存する。
    [ObservableProperty]
    private double _windowWidth;

    [ObservableProperty]
    private double _windowHeight;

    public MainViewModel(TodoFileReader fileReader, TodoCommandFileService commandFileService, AppSettingsService settingsService, AppSettings settings, ClaudeCompletionWatcher completionWatcher)
    {
        _fileReader = fileReader;
        _commandFileService = commandFileService;
        _completionWatcher = completionWatcher;
        _settingsService = settingsService;
        _rootParentDir = settings.RootParentDir;
        _loadedRoot = fileReader.LoadOrCreateRoot(_rootParentDir);
        _selectedTodo = _loadedRoot;

        _changeTracker = new TodoChangeTracker(_rootParentDir);
        _changeTracker.Attach(_loadedRoot);

        RootNode = new TodoNodeViewModel(_selectedTodo, _commandFileService, _completionWatcher);
        RebuildBreadcrumbs();

        _windowWidth = settings.WindowWidth;
        _windowHeight = settings.WindowHeight;
    }

    partial void OnWindowWidthChanged(double value) => MarkSettingsDirty();

    partial void OnWindowHeightChanged(double value) => MarkSettingsDirty();

    private void MarkSettingsDirty() => _settingsDirty = true;

    /// <summary>
    /// ウィンドウサイズに変更があれば⑤アプリ全体設定を保存する
    ///
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void SaveSettingsIfDirty()
    {
        if (!_settingsDirty)
            return;

        _settingsDirty = false;
        _settingsService.Save(new AppSettings(_rootParentDir, WindowWidth, WindowHeight));
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
        RootNode.Dispose();
        RootNode = new TodoNodeViewModel(newValue, _commandFileService, _completionWatcher);
        OnPropertyChanged(nameof(RootNode));
        RebuildBreadcrumbs();
    }

    // パンくずリストを組み立てる。
    // ①ファイルシステム上の祖先(ロード済みツリーの外側、フォルダ名の規則+④保存情報の存在で判定)。
    // ②インメモリの祖先(ロード済みツリー内、移動リンクで下りてきた分)。
    // ③現在表示中のTODO自身(リンク無し)。
    // ④再帰的に表示中のTODO項目(TodoItemのSelectedChildTodo/SelectedTabIndexを辿って算出)。
    // ファイルの中身は見ない。
    private void RebuildBreadcrumbs()
    {
        var entries = new List<BreadcrumbEntry>();
        var candidate = _rootParentDir;

        while (TodoFileNaming.ParseTodoFolderName(Path.GetFileName(candidate)) is { } parsed &&
               File.Exists(Path.Combine(candidate, TodoFileNaming.SaveInfoFileName)))
        {
            var parentDir = Path.GetDirectoryName(candidate);
            if (parentDir is null)
                break;

            entries.Add(new BreadcrumbEntry(parsed.Title, TargetTodo: null, parentDir));
            candidate = parentDir;
        }

        entries.Reverse();

        var inMemoryAncestors = new List<TodoItem>();
        for (var current = _changeTracker.GetParent(SelectedTodo); current is not null; current = _changeTracker.GetParent(current))
        {
            inMemoryAncestors.Add(current);
        }

        inMemoryAncestors.Reverse();
        entries.AddRange(inMemoryAncestors.Select(t => new BreadcrumbEntry(t.Title, t, ParentDir: null)));

        entries.Add(new BreadcrumbEntry(SelectedTodo.Title, TargetTodo: null, ParentDir: null));

        var displayedChain = new List<TodoItem> { SelectedTodo };
        var displayed = SelectedTodo;
        while (displayed.ChildTodoList.Count > 0 && displayed.SelectedTabIndex == 0 && displayed.SelectedChildTodo is { } child)
        {
            entries.Add(new BreadcrumbEntry(child.Title, child, ParentDir: null));
            displayedChain.Add(child);
            displayed = child;
        }

        // 末端がメモ情報を表示している場合(状況①②、または状況③でNOTEリスト選択時)は、
        // そのメモのタイトルもリンク無しで追加する。
        MemoItem? displayedMemo = null;
        if ((displayed.ChildTodoList.Count == 0 || displayed.SelectedTabIndex == 1) && displayed.SelectedMemo is { } memo)
        {
            displayedMemo = memo;
            entries.Add(new BreadcrumbEntry(memo.Title, TargetTodo: null, ParentDir: null));
        }

        if (entries.Count > 0)
            entries[^1] = entries[^1] with { IsLast = true };

        UpdateDisplayedChainSubscriptions(displayedChain, displayedMemo);
        Breadcrumbs = entries;
    }

    // 再帰的に表示中のTODO項目・メモ情報が変わりうる箇所(タイトル・子TODO一覧・選択状態)を
    // 購読し、変化があればパンくずリストを組み直す。
    private void UpdateDisplayedChainSubscriptions(List<TodoItem> chain, MemoItem? memo)
    {
        foreach (var item in _displayedChain)
        {
            item.PropertyChanged -= OnDisplayedChainItemPropertyChanged;
            item.ChildTodoList.CollectionChanged -= OnDisplayedChainChildListChanged;
        }

        _displayedChain.Clear();
        _displayedChain.AddRange(chain);

        foreach (var item in _displayedChain)
        {
            item.PropertyChanged += OnDisplayedChainItemPropertyChanged;
            item.ChildTodoList.CollectionChanged += OnDisplayedChainChildListChanged;
        }

        if (_displayedMemo is not null)
            _displayedMemo.PropertyChanged -= OnDisplayedMemoPropertyChanged;

        _displayedMemo = memo;

        if (_displayedMemo is not null)
            _displayedMemo.PropertyChanged += OnDisplayedMemoPropertyChanged;
    }

    private void OnDisplayedChainItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TodoItem.Title) or nameof(TodoItem.SelectedChildTodo)
            or nameof(TodoItem.SelectedTabIndex) or nameof(TodoItem.SelectedMemo))
            RebuildBreadcrumbs();
    }

    private void OnDisplayedChainChildListChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildBreadcrumbs();

    private void OnDisplayedMemoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemoItem.Title))
            RebuildBreadcrumbs();
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
        UpdateDisplayedChainSubscriptions([], null);
        RootNode.Dispose();
        _changeTracker.Dispose();
        _loadedRoot.Dispose();
        GC.SuppressFinalize(this);
    }
}
