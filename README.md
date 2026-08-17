# TodoApp

WPF (.NET) + MVVM (CommunityToolkit.Mvvm) で作成する階層型TODO管理デスクトップアプリです。

開発はアジャイルのスプリント単位([SP1]、[SP2]、...)で進めています。
開発ルールは別リポジトリで一元管理しています。

- https://github.com/yamazawa/DevGuidelines

クローズ済みスプリントの仕様書・実装方針(実装に反映済み)は以下を参照してください。

- `docs/archive/SP1/TODOアプリ_仕様書.md`
- `docs/archive/SP1/TODOアプリ_実装方針.md`
- `docs/archive/SP2/TODOアプリ_仕様書.md`
- `docs/archive/SP2/TODOアプリ_実装方針.md`

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
