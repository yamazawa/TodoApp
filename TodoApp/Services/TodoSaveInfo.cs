namespace TodoApp.Services;

/// <summary>
/// ④保存情報(TODO単位の表示用情報)のシリアライズ用DTO
///
/// 選択中の子TODO/メモ情報は、参照ではなくインデックスで保持する。
/// </summary>
public sealed record TodoSaveInfo(int SelectedTabIndex, int? SelectedChildIndex, int? SelectedMemoIndex);
