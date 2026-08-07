# TodoApp

WPF (.NET) + MVVM (CommunityToolkit.Mvvm) で作成したシンプルなTODO管理デスクトップアプリです。

## 機能

- TODOの追加・編集・削除
- 完了/未完了の切り替え
- すべて/未完了/完了済みでの絞り込み表示
- 完了済みTODOの一括削除
- `%AppData%\TodoApp\todos.json` への自動保存・読込

## 動作環境

- .NET 9 (Windows)

## 実行方法

```powershell
dotnet run --project TodoApp
```

## ビルド方法

```powershell
dotnet build
```

## 構成

```
TodoApp/
  TodoApp.sln
  TodoApp/
    Models/        TODOアイテムのモデル
    ViewModels/     MainViewModel など
    Services/       JSON永続化サービス
    Converters/     XAML用コンバーター
    MainWindow.xaml メイン画面
```
