# lazyFade Release作成

公開先の設定・添付対象は[VPM公開手順](../docs/publishing.md)、本文原稿は[0.2.0.md](0.2.0.md)を参照してください。

## Unitypackageを再作成する

PowerShell 7.4以上、Unity 2022.3.22f1と有効なEditorライセンスを使います。参照元はSDK／NDMF／lilToon／Unity Test Frameworkを導入済みの信頼できるAvatarプロジェクトです。

```powershell
./scripts/Prepare-Verification.ps1 -ReferenceProject 'C:/path/to/avatar-project'
./scripts/Prepare-NonToonVerification.ps1 -ShaderCoreSource 'C:/path/to/Shader-Core' -NonToonSource 'C:/path/to/NonToon'
./scripts/Run-Tests.ps1 -IncludeNonToon
./scripts/Test-VerificationSafety.ps1
./scripts/Test-ReleasePipeline.ps1
./scripts/Build-UnityPackage.ps1
./scripts/Test-PublicationSafety.ps1
```

検証環境を準備済みならPrepareの再実行は不要です。ShaderCore 0.1.11、NonToon 0.1.3を使用します。UnityパスはRun-Tests.ps1とBuild-UnityPackage.ps1の`-UnityEditor`で指定できます。

既定で両版を作ります。片方なら`-Edition lilToon`／`-Edition NonToon`を指定します。ビルド処理はテストを代行しないため、先にテストを通します。同名成果物があると停止します。公開済みなら版を上げ、未公開の再試行なら旧出力を保管して実行します。

## 出力・検証

`artifacts/<version>/`へ各unitypackage、RELEASE_NOTES.md、SHA256SUMS.txtを出力します。VPM生成コマンドで同じ場所へ両ZIPが追加され、ハッシュ一覧が更新されます。

版ごとに独立した`.verification~/release-<ID>/`を作り、Assets/lazyFade-lilToonまたはAssets/lazyFade-NonToonへ配置します。AssetDatabase.ExportPackageには依存関係追加を指定せず、製品フォルダーだけを出力します。

出力前にコンポーネントのコンパイルを確認します。出力後は元の内容を同じ一時プロジェクトのAssets外へ退避し、unitypackageをインポートして別起動で再コンパイルを確認します。ファイル数と全ファイル・.metaのSHA256一致を確認してから成果物として保存します。

配布はRuntime／Editor／README／CHANGELOG／LICENSEと.metaだけです。Unitypackageにpackage.jsonは入りません。SDK、NDMF、Shader、テスト、検証コード・ログは入りません。VPM ZIPのみmanifestが入ります。

コピーとログは.verification~/に残ります。公開しないでください。旧0.1.0の記録は履歴であり、0.2.0の配布には使用しません。
