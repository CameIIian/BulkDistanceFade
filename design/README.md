# 設計・検証資料

旧`docs/`に置いていた設計書・検証記録を、この`design/`へ移動しました。利用者向けのGitHub Pages原稿は、新しい[docs/index.md](../docs/index.md)にあります。

| 資料 | 内容 |
| --- | --- |
| [基本仕様・設計書](lilToon_DistanceFade_Bulk_Setter_Spec.md) | 初版の設計と後から追加した仕様 |
| [マテリアル除外機能](Material_Exclusion_Spec.md) | 除外の一致規則・集計・テスト要件 |
| [マルチShader拡張仕様](MultiShader_DistanceFade_Extension_Spec.md) | Poiyomi・ShaderCore／NonToonへの将来設計。追加Shaderへの対応は未実装 |
| [実装・検証記録](Implementation_Validation.md) | 実装時の判断、実行済みのテスト、残る手動検証 |
| [公開前レビュー](Public_Release_Review.md) | 2026-09-09時点の公開前確認記録 |

過去の初期値・作成時点の状況を含む資料です。現在の操作説明は[README](../README.md)と`docs/`、現在の設定値は実装を参照してください。

GitHub Pagesの公開元は`docs/`です。このフォルダーはPagesの生成対象に含めませんが、GitHubリポジトリ自体を公開した場合はソースとして閲覧できます。
