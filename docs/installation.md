---
layout: default
title: "導入"
description: "BulkDistanceFadeのインストール手順。"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# 導入

1. Unity 2022.3のVRChat Avatarプロジェクトを用意し、VRChat SDK Avatars・NDMF・lilToonを先に導入します。確認済みのバージョンは[対応環境](environment.html)を参照してください。
2. 次のいずれかの方法で本パッケージを導入します。
   - リポジトリの `Packages/com.camellian.liltoon-distance-fade` フォルダーを、導入先プロジェクトの同じ `Packages/com.camellian.liltoon-distance-fade` の位置へコピーします。
   - UnityのPackage Managerで「Add package from disk」を選び、本パッケージの `package.json` を指定します。
3. Unityのコンパイルが完了し、Consoleにコンパイルエラーがないことを確認します。
4. Add Componentの `BulkDistanceFade → Distance Fade Bulk Setter` からコンポーネントを選択します。

VPM依存情報は `package.json` に含まれますが、公開VPMリポジトリへの登録は行っていません。上記の手動導入では、VPM依存パッケージが自動導入されることを前提にせず、先に依存環境を用意してください。

導入が完了したら[使い方](usage.html)へ進んでください。

