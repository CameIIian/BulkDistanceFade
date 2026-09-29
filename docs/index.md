---
layout: default
title: lazyFade
description: VRChatアバターの距離フェードをまとめて設定するUnity Editor拡張
---

VRChatアバターの距離フェードを、NDMFビルド時の成果物にだけ設定。

## 導入

Unity 2022.3のアバタープロジェクトで、使用するShaderに合った版を追加すること。
共通でSDK Avatars >= 3.10.3, NDMF >= 1.13.1が必要。

| shader | versions |
| --- | --- |
| lilToon | lilToon >= 2.3.2 |
| NonToon | ShaderCore >= 0.1.11, NonToon >= 0.1.3以上 |


1. プロジェクトをバックアップ、依存関係をVPMで登録：[MA](https://modular-avatar.nadena.dev/ja/docs/intro)・[liltoon, nontoon, etc](https://lilxyzw.github.io/vpm-repos/vpm.json)。
2. VCC / ALCOM 等の **Settings → Packages → Add Repository** に、次のURLを登録。

   ```text
   https://cameiiian.github.io/lazyFade/index.json
   ```

3. **Manage Project** から **lazyFade - lilToon** または **lazyFade - NonToon** を追加し、Unityを開く。

<p class="action-row"><a class="button" href="vcc://vpm/addRepo?url=https%3A%2F%2Fcameiiian.github.io%2FlazyFade%2Findex.json">VCC / ALCOM等にリポジトリを追加</a></p>

## 使い方

1. Hierarchyでアバターのルートを選び、**Add Component → lazyFade → lazyFade lilToon / lazyFade NonToon** を追加。配置先はVRC Avatar Descriptorと同じGameObject。1個ずつ併用可能。
2. 変更したい項目のみ左のチェックをONにして、値を設定。
3. 必要に応じて「除外マテリアル」を指定。必要であれば「再集計」から対象やエラーを確認。
4. VRC向けにアップロード、及びGestureManager等のPreviewModeに入ると自動で適用。

## 困ったとき

| 症状 | 確認すること |
| --- | --- |
| コンポーネントが追加できない | 依存パッケージを導入し、Consoleのコンパイルエラーを解消する。 |
| ビルドしても変化しない | 処理が有効か、変更する項目のチェックがONか、対象が除外されていないかを確認すること。 |
| 配置・重複エラー | 上の手順1の配置先と個数を確認すること。無効なコンポーネントも個数に含まれる。 |

解決しない場合は、使用バージョンとConsoleのエラーを添えて[Issue](https://github.com/CameIIian/lazyFade/issues)まで。

lilToon, NonToon公式の拡張ではありません。設定の詳細や検証範囲、開発手順は[技術文書の目次](https://github.com/CameIIian/lazyFade/blob/main/docs/README.md)から確認できます。
