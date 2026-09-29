# Changelog

## 0.2.0 — 2026-09-29

- lazyFadeへ改名。パッケージID・コンポーネント名・表示名・配布名を整理。
- .meta GUIDと保存フィールドを維持し、旧コンポーネント型にMovedFromを指定。
- lilToon／NonToonを個別のunitypackage・VPM ZIPとして配布。
- VPM一覧生成、ソースと配布アーカイブの安全チェック、導入・技術資料を追加。

## 0.1.0 - 2026-09-10

- lilToon用と併設できる独立したNonToon用コンポーネントを追加。
- ShaderCore 0.1.11／NonToon 0.1.3の公式生成ShaderとDistance Fadeモジュールを識別し、公開APIでProperty名を解決。
- 近距離・遠距離・強度の項目別上書き、3件の編集可能なプリセット、マテリアル除外、集計を実装。
- NDMFによる非破壊複製、共有保持、保存失敗時の参照保持と生成物破棄に対応。
- 実Shader・NDMF併設・Inspector描画の回帰テストを追加。
