---
layout: default
title: "lazyFade"
description: "VRChatアバターのlilToon／NonToon距離フェードを非破壊で一括設定"
---

# index.md

![](./img/logo.png)

VRChatアバターの距離フェードを、NDMFビルド時にまとめて設定。
元マテリアルを変更せず、ビルド用の複製に適用。

## 使い方

1. Hierarchyで、**VRC Avatar Descriptorと同じアバタールートGameObject**を選択。
2. `lazyFade → lazyFade XXX` を1個追加。

## トラブルシューティング

| 症状・表示 | 確認・対処 |
| --- | --- |
| 他ツールの処理後に除外が効かない | Materialが別参照へ置換され、NDMFへ起源が登録されていない可能性があります。同名かどうかだけでは追跡しません。 |
| E001：配置エラー | VRC Avatar Descriptorと同じGameObjectへコンポーネントを配置します。子への配置は不可です。 |
| E002：重複エラー | 非アクティブ・無効コンポーネントも含め、アバター全体で設定を1個にします。 |

| ガイド | 内容 |
| --- | --- |
| [導入](installation.html) | VPM・unitypackage・必要な依存 |
| [lilToonの使い方](usage.html)／[設定](settings.html) | 操作、初期値、上書き、集計 |
| [NonToonの使い方](nontoon.html) | 3項目の設定とモジュール要件 |
| [トラブルシューティング](troubleshooting.html) | エラーと対処 |
