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
    private string _rootParentDir;
    private bool _settingsDirty;

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

    public MainViewModel(TodoFileReader fileReader, TodoCommandFileService commandFileService, AppSettingsService settingsService, AppSettings settings)
    {
        _fileReader = fileReader;
        _commandFileService = commandFileService;
        _settingsService = settingsService;
        _rootParentDir = settings.RootParentDir;
        _selectedTodo = fileReader.LoadOrCreateRoot(_rootParentDir);
        RootNode = new TodoNodeViewModel(_selectedTodo);
        RootNode.PropertyChanged += OnRootNodePropertyChanged;
        RebuildChildNode();
        RebuildBreadcrumbs();

        _topBottomRatio = settings.TopBottomRatio;
        _bottomLeftRightRatio = settings.BottomLeftRightRatio;
        _nestedTopBottomRatio = settings.NestedTopBottomRatio;
        _nestedLeftRightRatio = settings.NestedLeftRightRatio;

        _changeTracker = new TodoChangeTracker(_rootParentDir);
        _changeTracker.Attach(_selectedTodo);
    }

    partial void OnTopBottomRatioChanged(double value) => _settingsDirty = true;

    partial void OnBottomLeftRightRatioChanged(double value) => _settingsDirty = true;

    partial void OnNestedTopBottomRatioChanged(double value) => _settingsDirty = true;

    partial void OnNestedLeftRightRatioChanged(double value) => _settingsDirty = true;

    /// <summary>
    /// 比率に変更があれば⑤アプリ全体設定を保存する
    ///
    /// 定期保存とアプリ終了時の両方から呼ばれる。
    /// </summary>
    public void SaveSettingsIfDirty()
    {
        if (!_settingsDirty)
            return;

        _settingsDirty = false;
        _settingsService.Save(new AppSettings(_rootParentDir, TopBottomRatio, BottomLeftRightRatio, NestedTopBottomRatio, NestedLeftRightRatio));
    }

    /// <summary>
    /// パンくずリストの親TODOへ遷移する
    ///
    /// 現在のツリーの保留中の変更をキューへ積んでから切り替える。
    /// </summary>
    [RelayCommand]
    private void NavigateToParent(string parentDir)
    {
        EnqueuePendingChanges();

        var oldSelectedTodo = SelectedTodo;
        _changeTracker.Dispose();

        _rootParentDir = parentDir;
        SelectedTodo = _fileReader.LoadOrCreateRoot(_rootParentDir);
        _changeTracker = new TodoChangeTracker(_rootParentDir);
        _changeTracker.Attach(SelectedTodo);

        oldSelectedTodo.Dispose();
        _settingsDirty = true;
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

    // ファイル構成の親を辿り、TODOフォルダ名の規則+README存在の2条件で親TODOを判定する。
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

            entries.Add(new BreadcrumbEntry(parsed.Title ?? Strings.Breadcrumb_NullTitle, parentDir));
            candidate = parentDir;
        }

        entries.Reverse();
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
    public void Dispose()
    {
        ChildNode?.Dispose();
        RootNode.PropertyChanged -= OnRootNodePropertyChanged;
        RootNode.Dispose();
        _changeTracker.Dispose();
        SelectedTodo.Dispose();
        GC.SuppressFinalize(this);
    }
}
