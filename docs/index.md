---
layout: default
title: "BulkDistanceFade"
description: "lilToon向けの非破壊な距離フェード一括設定ツール。"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# BulkDistanceFade

## 概要

BulkDistanceFadeは、VRChatアバターで使用するlilToonマテリアルの距離フェード設定を、アバタールートの1つのコンポーネントから一括適用するUnity Editor拡張です。NDMFのビルド処理でマテリアルを複製し、その複製へ設定を書き込みます。

lilToon公式の機能ではなく、lilToonに対応する独立した拡張です。現在の実装対象はlilToonであり、Poiyomi・ShaderCore／NonToonには対応していません。

パッケージIDは `com.camellian.liltoon-distance-fade`、`package.json`のバージョンは `0.1.0` です。コードのライセンスは[MIT License](license.html)です。依存パッケージにはそれぞれのライセンスが適用されます。

## 特徴

- **項目ごとの上書き指定**：色、距離、強度、裏面、モード、リムの各項目に「適用」チェックがあります。OFFの項目はマテリアルの既存値を保持します。
- **元アセットを保持**：本ツールは元Material・Prefab・Textureへ変更を保存しません。生成するMaterialはビルド用です。
- **アバター全体を処理**：ルートと全子孫のMeshRenderer／SkinnedMeshRendererを走査します。非アクティブGameObjectと無効Rendererも対象です。
- **マテリアルの共有を保持**：同じ対象Materialを使うスロットには、1個の複製を共有して割り当てます。
- **マテリアル単位の除外**：指定したMaterialを使うすべての対象スロットを除外できます。
- **編集時の集計と入力検証**：対象件数・除外件数、配置エラー、非対応項目をInspectorで確認できます。

## ガイド

| ページ | 内容 |
| --- | --- |
| [導入](installation.html) | 依存パッケージとインストール手順 |
| [使い方](usage.html) | コンポーネントの追加からビルドまで |
| [設定項目](settings.html) | 全操作項目・初期値・集計表示 |
| [対応環境](environment.html) | 依存範囲、確認済みの環境、未検証事項 |
| [注意事項](notes.html) | 対象範囲と機能の制限 |
| [トラブルシューティング](troubleshooting.html) | 症状別の対処とエラーコード |
| [Build / Test](development.html) | ビルド・テストと開発時の編集箇所 |

設定・初期値は現在の実装、検証状況は2026-09-09の記録に基づきます。設計書と検証記録はリポジトリの `design/` に格納しています。

[このドキュメントの公開・更新方法](publishing.html) · [ライセンス](license.html)

