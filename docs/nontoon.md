---
layout: default
title: "NonToon版の使い方"
---

# lazyFade - NonToon

[導入](installation.md)後、VRC Avatar Descriptorと同じGameObjectへ **lazyFade → lazyFade NonToon** を追加します。ShaderCore 0.1.11以上、NonToon 0.1.3以上のPackages形式を対象にしています。

## 設定

初期状態では適用チェックがすべてOFFです。変更する行をONにしてください。

| 項目 | 初期値 | 条件 |
| --- | --- | --- |
| 近距離境界 | 0.01／適用OFF | 0以上、遠距離より小さい値 |
| 遠距離境界 | 0.1／適用OFF | 近距離より大きく、1以下 |
| 強度 | 0／適用OFF | 0～1。0を適用すると減光を無効化 |
| 除外マテリアル | 空 | 指定Materialの全スロットを除外 |
| 未対応NonToonでビルドを停止 | OFF | OFFなら警告してスキップ、ONならエラー |

近距離と遠距離は、適用OFFで保持するMaterial値と合成した最終値でも **0 ≤ 近距離 < 遠距離 ≤ 1** が必要です。自動の並べ替え・Clampはしません。

「再集計」で対象Renderer・一意Material・除外Material／Slot・未対応理由を確認します。集計はプレビューではなく、NDMFビルドで初めて複製へ適用します。ビルド前に再集計しなくても最新値を使用します。

## Shaderとプリセット

Distance FadeモジュールをShader側に組み込んでください。本ツールはモジュール追加、Shader再生成、キーワード同期を行いません。公式NonToon／NonToonFurの生成元GUID、導入版、モジュール属性、Property型を検査します。出自不明の独自Shaderや定数化で書き込み先が失われた構成は対象外です。

NonToonのheadDepthによる距離を用いてRGBを黒へ減光します。距離Vectorのz/wを保持し、強度は独立Propertyへ書き込みます。lilToon版の色・リム・裏面・モードは提供しません。

プリセット選択は近距離・遠距離・強度の保存値だけを上書きし、適用チェック・処理有効・除外・高度な設定を保持します。定義はソースの`Editor/NonToonPresets.cs`です。現在は3件の仮名・同値（0.01、0.1、0）なので必要に応じて編集します。

## エラー・制限

| コード／症状 | 対処 |
| --- | --- |
| NT001 | VRC Avatar Descriptorと同じGameObjectへ配置 |
| NT002 | 無効状態を含めNonToon用を1個にする |
| NT003 | 適用値と保持値の合成結果が距離・強度条件を満たすか確認 |
| NT004 | 対応版・生成元・モジュール・Property・定数化を確認 |
| 対象0件 | 処理有効、適用チェック、強度、除外、Shader要件を確認 |

非アクティブ・無効を含むMeshRenderer／SkinnedMeshRendererを走査します。AnimationClipだけが参照するMaterial、ParticleSystemRenderer、MaterialPropertyBlockは対象外です。AnimationClipの値変更で結果が上書きされる場合があります。

除外はNDMFに登録された起源を追跡し、同名という理由では一致させません。lilToon版の除外設定とは独立しています。実アップロード、VR・鏡・カメラ・Android、他ツール混在の実アバター確認は利用環境で行ってください。

技術設計はソースリポジトリの`design/lazyFade_NonToon_TechnicalGuide.md`と`design/lazyFade_NonToon_Spec.md`を参照してください。
