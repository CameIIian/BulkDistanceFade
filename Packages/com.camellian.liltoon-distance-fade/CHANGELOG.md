# Changelog

## Unreleased

- コンポーネント追加メニューを`BulkDistanceFade/Distance Fade Bulk Setter`へ変更し、Inspector・NDMF・Package Managerの表示名を`BulkDistanceFade`に統一。

- Inspectorへマテリアル除外リストを追加。共有スロットをまとめて除外し、対象・除外件数を表示。
- NDMFに登録済みの置換元情報を使い、先行処理によるMaterial置換後も除外を追跡。
- 除外・非破壊性・NDMF連携・シリアライズ互換性・Undo／Redoの回帰テストを追加。

## 0.1.0 - 2026-09-08

- アバタールート専用の距離フェード設定コンポーネントを追加。
- HDR Color、Vector4要素別上書き、Mode、Rim設定に対応。
- NDMFによる非破壊Material複製、共有保持、出力からの設定除去を実装。
- 専用Inspector、読み取り専用集計、配置・入力検証、NDMF診断を追加。
- Edit Modeの回帰テストを追加。
