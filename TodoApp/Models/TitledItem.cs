using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Resources;

namespace TodoApp.Models;

/// <summary>
/// タイトルを持つ項目の共通基底クラス
///
/// TodoItem、MemoItemの両方から使う。
/// </summary>
public abstract partial class TitledItem : ObservableObject, IDisposable
{
    private string _title = Strings.Title_Default;

    /// <summary>
    /// タイトル(必須)
    ///
    /// 空文字は無視し、直前の値を保持する。
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrEmpty(value) || value == _title)
                return;

            SetProperty(ref _title, value);
        }
    }

    /// <summary>
    /// リスト項目が編集中かどうか
    ///
    /// 表示専用の一時的な状態で、ファイルには保存しない。
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    [RelayCommand]
    private void StartEdit() => IsEditing = true;

    [RelayCommand]
    private void EndEdit() => IsEditing = false;

    // 既定では解放するものが無い。イベント購読を持つ派生クラスでオーバーライドする。
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
