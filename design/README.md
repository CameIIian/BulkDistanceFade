# lazyFade 設計・検証資料

現在版は0.2.0です。以下の詳細ガイドと移行・配布仕様を優先してください。2026-09-10以前の章は設計履歴を含みます。

- [lilToon技術ガイド](lazyFade_lilToon_TechnicalGuide.md)：全設定、部分上書き、除外、集計、プリセット。
- [NonToon技術ガイド](lazyFade_NonToon_TechnicalGuide.md)：モジュール判定、3項目の値検証、Property解決。
- [名称変更・互換性](../docs/migration.md)：ID／型／GUID／保存フィールドと移行手順。
- [配布仕様](../docs/publishing.md)・[安全検査](../docs/security.md)：ZIP構造、一覧、再現性、公開対象。

2026-09-10整理。統合していた追加Shader設計を、ShaderCore＋NonToon用とPoiyomi用に置き換えました。lilToonで完成した機能は共通の実装参照資料へ整理し、各設計書から参照します。現在はlilToon用とNonToon用を別パッケージ・別コンポーネントとして提供します。Poiyomi用は未実装です。

## これから追加Shaderを実装するとき

まず実装参照資料で現在の動作と共通要件を確認し、その後、対象Shaderの設計書を読んでください。2つの対応は個別に着手できます。

| 資料 | 状態・内容 |
| --- | --- |
| [lilToon実装の参照資料と共通設計](lilToon_Implementation_Reference.md) | 0.1.0のコード対応表、初期値、UI、プリセット、除外、61件のテスト実績。追加Shader向けの共通構成と受入条件も区別して記載 |
| [ShaderCore＋NonToon 距離フェード設計](lazyFade_NonToon_Spec.md) | 段階A実装済み。別コンポーネントの3項目・除外・NDMF適用。モジュール自動追加は未実装 |
| [Poiyomi Proximity Color対応設計](lazyFade_Poiyomi_Spec.md) | 未実装。固有の色・距離・位置基準、ロックとアニメーション名、対応版の確認条件と試験 |

公式資料で確認した内容、旧調査から引き継いだ候補、今後実装する要件を各文書で区別しています。可変ブランチやWeb資料だけでは、対応バージョンの動作保証になりません。

## lilToonの設計履歴

| 資料 | 役割 |
| --- | --- |
| [初版仕様・設計書](lazyFade_lilToon_Spec.md) | 初版の判断と原資料。初期値・UI等は作成時点の記述を含む |
| [マテリアル除外機能](Material_Exclusion_Spec.md) | 実装済みの一致規則・集計・非破壊性。画面配置は後続のUI変更あり |

## 検証と公開の記録

| 資料 | 役割 |
| --- | --- |
| [実装・検証記録](Implementation_Validation.md) | 機能追加ごとのテスト実績、Unitypackage作成・再インポート検証、残る手動確認 |
| [公開前レビュー](Public_Release_Review.md) | 2026-09-09のソース・機密情報・依存関係の確認範囲と制限 |
| [リリース資料と再作成手順](../release/README.md) | 配布用Unitypackageの作成方法と日本語リリース原稿 |

現在の利用者向け操作説明は[README](../README.md)と[GitHub Pages原稿](../docs/index.md)、実値はRuntime／Editorのコードを参照してください。設計履歴と現在の操作説明が違う場合は、実装参照資料と実装・検証記録で変更経緯を確認できます。

GitHub Pagesの公開元は`docs/`です。`design/`はPagesの生成対象外ですが、リポジトリを公開した場合はソースとして閲覧できます。
