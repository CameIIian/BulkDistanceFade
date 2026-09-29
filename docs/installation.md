---
layout: default
title: "導入"
---

# lazyFadeの導入

Unity 2022.3のVRChat Avatarプロジェクトをバックアップしてから導入します。旧版が入っている場合は[移行手順](migration.md)を先に確認してください。

## 依存パッケージ

| 版 | 共通の依存 | Shader |
| --- | --- | --- |
| lilToon | SDK Avatars 3.10.3以上・4未満、NDMF 1.13.1以上・2未満 | lilToon 2.3.2以上・3未満 |
| NonToon | 同上 | ShaderCore **0.1.11以上**、NonToon **0.1.3以上** |

これらはmanifestの依存範囲です。全範囲を実機検証した意味ではありません。確認済み環境は[対応環境](environment.md)を参照してください。NonToon版はShaderCore／NonToonをPackages形式で導入します。

## VPM（公開後）

1. VCCのSettings → Packages → Add Repositoryへ、作者が公開したlazyFadeのindex.json URLを追加します。公開を行う方は[公開手順](publishing.md)でURLを生成してください。
2. 依存の配布元リポジトリも登録します。NDMFは[Modular Avatarの公式導入案内](https://modular-avatar.nadena.dev/ja/docs/intro)からnadenaのリポジトリを登録できます。lilToon／ShaderCore／NonToonは[lilxyzwのVPMリポジトリ](https://lilxyzw.github.io/vpm-repos/vpm.json)を登録します。Modular Avatar本体はlazyFadeの必須依存ではありません。
3. プロジェクトのManage Projectで **lazyFade - lilToon** または **lazyFade - NonToon** を追加します。
4. Unityを開き、Consoleにコンパイルエラーがないことを確認します。

依存リポジトリ未登録ではVPMが必要な版を解決できない場合があります。依存パッケージはlazyFadeのZIPへ同梱していません。

## Unitypackage

1. 依存を先に導入します。
2. Releaseから使う版の`lazyFade-lilToon-0.2.0.unitypackage`または`lazyFade-NonToon-0.2.1.unitypackage`を入手します。
3. UnityのAssets → Import Package → Custom Packageで全項目インポートします。導入先はAssets/lazyFade-lilToonまたはAssets/lazyFade-NonToonです。

同じ版のAssets版とVPM／Packages版を同時に導入しないでください。GUIDとassemblyが重複します。lilToon版とNonToon版の併設は可能です。

## ソースからの手動導入

依存を導入済みのプロジェクトへ、対象の`Packages/com.camellian.lazyfade.liltoon`または`Packages/com.camellian.lazyfade.nontoon`フォルダーをコピーできます。Unity Package ManagerのAdd package from diskでpackage.jsonを指定する方法もあります。UnityのUPMはvpmDependenciesを自動解決する前提ではありません。

VRC Avatar Descriptorと同じGameObjectへ **lazyFade → lazyFade lilToon / lazyFade NonToon** を追加します。[lilToonの使い方](usage.md)・[NonToonの使い方](nontoon.md)へ進んでください。
