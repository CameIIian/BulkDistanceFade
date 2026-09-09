---
layout: default
title: "注意事項"
description: "距離フェード一括適用の範囲と制限。"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# 注意事項

- 元Materialへの恒久的な書き込み、Prefab Apply、Sceneの即時プレビュー、実行時に設定を操作するUIは提供しません。
- 対象はルート以下のMeshRenderer／SkinnedMeshRendererの `sharedMaterials` です。ParticleSystemRendererなどの別種Renderer、アバター外のRenderer、未使用Materialは処理しません。
- AnimationClip内のMaterialプロパティやMaterial差し替えキーは変更しません。Clipだけが参照するMaterialは走査対象外です。アニメーション再生で、適用した値が上書きされる場合があります。
- MaterialPropertyBlock、Shaderキーワード、描画モードは変更しません。Propertyが存在しても、Shaderの機能削減設定などによって距離フェードが描画されない場合があります。
- 一部のPropertyだけが不足する場合は警告し、対応する項目を適用します。適用できるPropertyが1つもなければ、そのMaterialを複製しません。
- 初期状態で左端のチェックがONの項目は、元Materialの設定を上書きします。現在の初期強度は0.95です。初期リム色のAlphaは0なので、既存の距離フェード用リム設定を残すにはリムの左端のチェックをOFFにしてください。
- コードの初期値を変更しても、Scene／Prefabに保存済みの設定は一括更新されません。既存コンポーネントの値はInspectorで編集してください。
- 除外設定は本ツールだけに適用されます。他ツールによる複製・置換・統合・最適化を禁止するものではありません。
