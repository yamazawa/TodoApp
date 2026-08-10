using System.Collections.Specialized;

namespace TodoApp.Models;

/// <summary>
/// NotifyCollectionChangedEventArgsの拡張メソッド
///
/// 追加/削除された要素ごとの登録/解除処理を共通化する。
/// </summary>
public static class NotifyCollectionChangedEventArgsExtensions
{
    public static void ForEachAddedRemoved<T>(
        this NotifyCollectionChangedEventArgs e,
        Action<T> onAdded,
        Action<T> onRemoved)
    {
        if (e.OldItems is not null)
        {
            foreach (T item in e.OldItems)
            {
                onRemoved(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (T item in e.NewItems)
            {
                onAdded(item);
            }
        }
    }
}
