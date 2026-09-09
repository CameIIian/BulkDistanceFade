---
layout: default
title: "対応環境"
description: "宣言された依存範囲と検証済み環境。"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# 対応環境

package.json（リポジトリ内の `Packages/com.camellian.liltoon-distance-fade/package.json`）の宣言と、実際の確認環境を分けて記載します。

| 項目 | 宣言・要件 | API照合／自動テストの確認環境 |
| --- | --- | --- |
| Unity | 2022.3 | 2022.3.22f1 |
| VRChat SDK Avatars | `>=3.10.3 <4.0.0` | Base／Avatars 3.10.3 |
| NDMF | `>=1.13.1 <2.0.0` | 1.13.1 |
| lilToon | `>=2.3.2 <3.0.0` | 2.3.2 |
| Unity Test Framework | 自動テストを実行する場合に必要 | 1.4.6 |
| 検証プラットフォーム | Unity Editor拡張 | Windows Editor、Edit Mode |

Modular Avatar（MA）、TexTransTool（TTT）、Avatar Optimizer（AAO）は任意です。これらへの直接のassembly参照はありません。本ツールはNDMFのOptimizing段階で、MA本体・late transform・TTTの後、AAOの前に実行するよう順序を指定しています。

**依存バージョン範囲内の全組合せについて動作確認したわけではありません。** 実MA／TTT／AAO混在アバター、実際のVRChatアップロード、Play Mode移行時の見た目、Android等の環境、Inspectorの目視操作は手動検証が残っています。先行処理によるMaterial置換は、テスト用NDMFプラグインでも検証しています。

