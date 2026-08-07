using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// メイン画面のViewModel。TODOの追加・完了切替・削除・絞り込みを扱う。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ITodoStorageService _storageService;
    private readonly ICollectionView _todosView;

    public ObservableCollection<TodoItem> Todos { get; } = [];

    public ICollectionView TodosView => _todosView;

    [ObservableProperty]
    private string _newTodoTitle = string.Empty;

    [ObservableProperty]
    private TodoFilter _currentFilter = TodoFilter.All;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _completedCount;

    public MainViewModel(ITodoStorageService storageService)
    {
        _storageService = storageService;

        _todosView = CollectionViewSource.GetDefaultView(Todos);
        _todosView.Filter = FilterPredicate;
        if (_todosView is ICollectionViewLiveShaping liveShaping)
        {
            liveShaping.LiveFilteringProperties.Add(nameof(TodoItem.IsCompleted));
            liveShaping.IsLiveFiltering = true;
        }

        Todos.CollectionChanged += OnTodosCollectionChanged;
    }

    public async Task InitializeAsync()
    {
        var items = await _storageService.LoadAsync();
        foreach (var item in items)
        {
            Todos.Add(item);
        }

        UpdateCounts();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var title = NewTodoTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        Todos.Add(new TodoItem { Title = title });
        NewTodoTitle = string.Empty;
        await SaveAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(TodoItem? item)
    {
        if (item is null)
        {
            return;
        }

        Todos.Remove(item);
        await SaveAsync();
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        var completed = Todos.Where(t => t.IsCompleted).ToList();
        foreach (var item in completed)
        {
            Todos.Remove(item);
        }

        await SaveAsync();
    }

    partial void OnCurrentFilterChanged(TodoFilter value)
    {
        _todosView.Refresh();
    }

    private bool FilterPredicate(object obj)
    {
        if (obj is not TodoItem item)
        {
            return false;
        }

        return CurrentFilter switch
        {
            TodoFilter.Active => !item.IsCompleted,
            TodoFilter.Completed => item.IsCompleted,
            _ => true,
        };
    }

    private void OnTodosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TodoItem item in e.OldItems)
            {
                item.PropertyChanged -= OnTodoItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TodoItem item in e.NewItems)
            {
                item.PropertyChanged += OnTodoItemPropertyChanged;
            }
        }

        UpdateCounts();
    }

    private async void OnTodoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItem.IsCompleted))
        {
            UpdateCounts();
            await SaveAsync();
        }
    }

    private void UpdateCounts()
    {
        ActiveCount = Todos.Count(t => !t.IsCompleted);
        CompletedCount = Todos.Count(t => t.IsCompleted);
    }

    private Task SaveAsync() => _storageService.SaveAsync(Todos);
}
