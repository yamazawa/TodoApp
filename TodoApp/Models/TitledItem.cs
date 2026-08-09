using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TodoApp.Models;

/// <summary>
/// タイトル(null可)と本文を持つ項目の共通基底クラス
///
/// TodoItem、MemoItemの両方から使う。
/// </summary>
public abstract partial class TitledItem : ObservableObject
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string _body = string.Empty;

    /// <summary>
    /// リスト項目が編集中かどうか
    ///
    /// 表示専用の一時的な状態で、ファイルには保存しない。
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// 表示用タイトル
    ///
    /// タイトルがnullの場合は本文の1行目を返す。
    /// </summary>
    public string DisplayTitle => string.IsNullOrEmpty(Title) ? FirstLineOf(Body) : Title;

    partial void OnTitleChanged(string? value) => OnPropertyChanged(nameof(DisplayTitle));

    partial void OnBodyChanged(string value) => OnPropertyChanged(nameof(DisplayTitle));

    [RelayCommand]
    private void StartEdit() => IsEditing = true;

    [RelayCommand]
    private void EndEdit() => IsEditing = false;

    private static string FirstLineOf(string text)
    {
        var index = text.IndexOfAny(['\r', '\n']);
        return index < 0 ? text : text[..index];
    }
}
