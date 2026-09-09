---
layout: default
title: "使い方"
description: "コンポーネントの追加と距離フェードの適用手順。"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# 使い方

1. Hierarchyで、**VRC Avatar Descriptorと同じアバタールートGameObject**を選択します。
2. `BulkDistanceFade → Distance Fade Bulk Setter` を1個追加します。
3. 「処理を有効化」がONであることを確認します。
4. 各項目の「適用」をONにして、値を入力します。既存値を残したい項目は「適用」をOFFにします。
5. 必要に応じて「除外マテリアル」を開き、処理したくないMaterialを登録します。
6. 「集計を更新」を押し、対象件数・除外件数とエラー表示を確認します。
7. NDMFが実行されるアバタービルド、またはNDMFによるPlay Mode処理で結果を確認します。実アップロード・Play Modeの手動検証状況は[対応環境](environment.html)を参照してください。

**Inspectorの編集や「集計を更新」だけでは、Scene内のMaterialの見た目は変わりません。** 本ツールの適用タイミングはNDMFビルド処理です。手動で元Materialへ設定を焼き込むボタンはありません。

設定コンポーネントは1アバターにつき1個です。子オブジェクトに取り付けたり、別の子へ追加して設定を分けたりすることはできません。処理後のビルド出力からは設定コンポーネントを除去します。

各項目の入力方法と初期値は[設定項目](settings.html)を参照してください。

