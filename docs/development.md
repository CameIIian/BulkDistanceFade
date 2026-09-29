---
layout: default
title: "Build / Test"
description: "アバタービルド、自動テスト、開発時の編集箇所。"
---

[技術文書の目次](README.md) · [利用者向けガイド](https://cameiiian.github.io/lazyFade/)

---

# Build / Test

### アバターのビルド

本パッケージはUnityがC#をコンパイルするEditor拡張です。独立した実行ファイルを作るビルド手順はありません。設定はNDMFのアバタービルド処理で適用されます。VRChat SDKでビルドする前に、Inspectorの設定エラーとConsoleのコンパイルエラーを解消してください。

### 導入先プロジェクトでのテスト

Unity Test Frameworkを導入し、プロジェクトの `Packages/manifest.json` のルートへ以下の項目を追加します。既に `testables` がある場合は、その配列へ導入した版のパッケージIDを追加してください。両版をテストする場合の例です。

```json
"testables": ["com.camellian.lazyfade.liltoon", "com.camellian.lazyfade.nontoon"]
```

上記はmanifestへ追加する項目の断片であり、ファイル全体を置き換えるJSONではありません。

UnityのTest RunnerでEdit Modeを選び、`Camellian.DistanceFade.Tests.Editor` のテストを実行します。NonToon版のassemblyは`Camellian.NonToonDistanceFade.Tests.Editor`です。テストには対応するShaderとVRChat SDK、NDMFも必要です。配布用ZIP／unitypackageにはTestsを同梱しないため、テストにはソースリポジトリのPackagesを使用してください。

### リポジトリの独立検証プロジェクト

以下は、`scripts/` を含むリポジトリ全体を取得し、そのルートでPowerShellを実行する手順です。配布パッケージのフォルダー単体には、これらのスクリプトや `design/`・`docs/` は含まれません。

```powershell
./scripts/Prepare-Verification.ps1 -ReferenceProject 'C:/path/to/existing-avatar-project'
./scripts/Prepare-NonToonVerification.ps1 -ShaderCoreSource 'C:/path/to/Shader-Core' -NonToonSource 'C:/path/to/NonToon'
./scripts/Run-Tests.ps1 -IncludeNonToon
```

参照元は必要な依存パッケージとUnity Test Frameworkが導入済みの、信頼できるプロジェクトを指定します。スクリプトは依存パッケージ等を読み取り、リポジトリ内の `.verification~/Unity` へ検証用にコピーします。参照元プロジェクトは変更しません。シンボリックリンク／ジャンクション等は検証スクリプトの安全性チェックで拒否されます。

テスト実行スクリプトは既定で `C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe` を使用します。別のインストール先を使う場合は指定します。

```powershell
./scripts/Run-Tests.ps1 -UnityEditor 'C:/path/to/Editor/Unity.exe'
```

Unity Editorを実行できるライセンス環境が必要です。Inspector描画の回帰テストがあるため、グラフィックスが利用できる環境で実行し、`-nographics`は指定しません。結果XMLは `.verification~/editmode-results.xml`、Unityログは `.verification~/editmode.log` に出力します。結果が古い、0件、失敗、またはUnityが異常終了した場合、実行スクリプトはエラーを返します。

検証スクリプト自体の安全性に関する回帰テストは次で実行できます。

```powershell
./scripts/Test-VerificationSafety.ps1
```

### Unitypackageの作成

依存関係を用意したリポジトリのルートで、コードのテスト後に実行します。

```powershell
./scripts/Build-UnityPackage.ps1
```

現在のバージョンでは `artifacts/0.2.0/` にUnitypackage、リリース説明、SHA256を出力します。独立した一時プロジェクトでAssets版をコンパイルし、Unityによる出力と再インポートの内容一致を検証します。SDK・NDMF・lilToon・Testsは同梱しません。同名の成果物は上書きしません。詳しい手順とリリース原稿はリポジトリ内の `release/README.md` にあります。

### 確認済みのテストと資料

2026-09-29のUnity 2022.3.22f1による実行で、**Edit Modeテスト90件合格（lilToon 62件、NonToon 28件）・失敗0件・スキップ0件**を確認しています。非破壊適用、共有Material、部分上書き、配置・入力検証、除外とNDMF置換追跡、Prefab保存・互換性、Undo／Redo、裏面とモードの初期適用OFFによる既存値保持、初期色のRGB正規化と色単位の初期化・Undo／Redo、除外リストを展開したInspectorの描画、集計未更新時の最新設定適用、プリセットの6値反映・既存オプション保持・不正値拒否を含みます。これは実アップロードや全対応環境の見た目を検証した結果ではありません。

リポジトリ全体に含まれる関連資料：

- 実装・検証記録（リポジトリ内の `design/Implementation_Validation.md`）
- 基本仕様・設計書（リポジトリ内の `design/lazyFade_lilToon_Spec.md`）
- マテリアル除外機能の設計書（リポジトリ内の `design/Material_Exclusion_Spec.md`）
- lilToon実装の参照資料と共通設計（リポジトリ内の `design/lilToon_Implementation_Reference.md`）
- ShaderCore＋NonToonの設計書・実装範囲（リポジトリ内の `design/lazyFade_NonToon_Spec.md`）
- Poiyomiの設計書（未実装）（リポジトリ内の `design/lazyFade_Poiyomi_Spec.md`）
- 公開前レビューの記録（リポジトリ内の `design/Public_Release_Review.md`）

### 設定画面・変数をコードで編集する場合

| 調整内容 | 編集箇所 |
| --- | --- |
| 組み込みプリセットの名前と6項目 | `Packages/com.camellian.lazyfade.liltoon/Editor/LazyFadeLilToonPresets.cs` の `BuiltIn` |
| コンポーネント追加メニュー、保存する変数・型・初期値 | Runtime/LazyFadeLilToon.cs（リポジトリ内の `Packages/com.camellian.lazyfade.liltoon/Runtime/LazyFadeLilToon.cs`） |
| Inspectorのラベル・並び・説明・入力欄 | Editor/LazyFadeLilToonEditor.cs（リポジトリ内の `Packages/com.camellian.lazyfade.liltoon/Editor/LazyFadeLilToonEditor.cs`）の `OnInspectorGUI`、`Field`、`ModeField` |
| 適用項目の識別、入力検証、ビルド用設定の取り込み | Editor/SettingsValidator.cs（リポジトリ内の `Packages/com.camellian.lazyfade.liltoon/Editor/SettingsValidator.cs`）の `Overrides`、`SettingsSnapshot`、`Capture` |
| ShaderのProperty名、対応確認、値の書き込み | Editor/MaterialUtility.cs（リポジトリ内の `Packages/com.camellian.lazyfade.liltoon/Editor/MaterialUtility.cs`） |

`Field("overrideStartDistance", "startDistance", "開始距離")` の引数は、順に「適用のbool変数」「値の変数」「表示ラベル」です。専用Inspectorのため、公開変数を追加するだけでは入力欄は表示されません。新しい適用項目を追加するときは、画面だけでなく取り込み・検証・書き込みも対応させます。

保存済み設定との互換性を保つには、既存フィールド名を変更する際に `FormerlySerializedAs` 等の移行対応が必要です。設定を変更した場合は、該当する回帰テストを実行してください。

ドキュメントの公開手順は[GitHub Pagesの公開・更新](publishing.md)を参照してください。

### 配布処理のテスト

PowerShell 7.4以上で実行します。Unityを使わず、`.verification~/publication-tests/`内のコピーに対してVPM生成・再生成・過去版保持・改ざん拒否・秘密検出を検証します。公開先の実URLには通信しません。

```powershell
./scripts/Test-ReleasePipeline.ps1
./scripts/Test-PublicationSafety.ps1
```

新版の互換性テストでは、旧Script GUIDと旧m_EditorClassIdentifierを持つPrefabが新型として読み込まれ、距離と適用チェックが保持されることを両版で確認します。元Material保持やNDMF併設テストと合わせて検証しています。