# BulkDistanceFade

lilToonの距離フェードをアバター全体へ非破壊で設定するNDMFプラグインです。

- [導入・使い方](Packages/com.camellian.liltoon-distance-fade/README.md)
- [仕様・設計書](docs/lilToon_DistanceFade_Bulk_Setter_Spec.md)
- [マテリアル除外機能の設計書](docs/Material_Exclusion_Spec.md)
- [Poiyomi・ShaderCore＋NonToonへの拡張仕様](docs/MultiShader_DistanceFade_Extension_Spec.md)
- [実装・検証記録](docs/Implementation_Validation.md)
- [GitHub Public公開前レビュー](docs/Public_Release_Review.md)

実装は`Packages/com.camellian.liltoon-distance-fade`にあります。設定コンポーネントはアバタールートに追加してください。

開発用の独立した検証プロジェクトは、依存パッケージが導入済みの既存プロジェクトを指定して準備できます。既存プロジェクトは読み取り元としてのみ使用します。

```powershell
./scripts/Prepare-Verification.ps1 -ReferenceProject 'C:/path/to/existing-avatar-project'
./scripts/Run-Tests.ps1
```

生成先は`.verification~/Unity`です。Unity 2022.3で開いてTest RunnerのEdit Modeテストを実行してください。依存パッケージのコピーは検証用であり、配布パッケージには含めません。

参照プロジェクトには信頼できる依存パッケージを使用してください。検証スクリプトはパス逸脱を防ぐため、シンボリックリンク／ジャンクションを含む参照先を拒否します。スクリプトの安全性に関する回帰テストは`./scripts/Test-VerificationSafety.ps1`で実行できます。
