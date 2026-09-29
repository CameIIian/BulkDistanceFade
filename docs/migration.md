---
layout: default
title: "lazyFadeへの移行"
---

# lazyFadeへの移行（0.2.0）

プロジェクトの表示名、パッケージID、C#コンポーネント型とファイル名を変更しました。距離フェードの適用ロジックと保存フィールドは変更していません。

| 対象 | 旧名 | 新名 |
| --- | --- | --- |
| lilToon package ID | `com.camellian.liltoon-distance-fade` | `com.camellian.lazyfade.liltoon` |
| NonToon package ID | `com.camellian.nontoon-distance-fade` | `com.camellian.lazyfade.nontoon` |
| lilToon component | `DistanceFadeBulkSetter` | `LazyFadeLilToon` |
| NonToon component | `NonToonDistanceFadeBulkSetter` | `LazyFadeNonToon` |
| lilToon Assetsフォルダー | `Assets/BulkDistanceFade` | `Assets/lazyFade-lilToon` |
| NonToon Assetsフォルダー | 旧配布なし | `Assets/lazyFade-NonToon` |

## 利用者の手順

1. Unityを閉じ、プロジェクト全体をバックアップします。
2. 旧版のツールフォルダーをプロジェクト外へ退避します。lilToon／SDK等の依存パッケージやアバターは外しません。Packages形式で旧版を参照していた場合は、`Packages/manifest.json` の旧パッケージ参照も外します。VCC管理の旧版はVCCで削除し、管理JSONを直接編集しないでください。
3. 新版をVPMまたはunitypackageのどちらかで導入します。異なる版（lilToonとNonToon）の併設は可能ですが、同じ版の旧新・Assets／Packagesの併設はできません。
4. Unityを開き、Consoleエラー、Missing Script、Scene／Prefab内のコンポーネント設定・マテリアル参照を確認してから保存します。
5. プロジェクトの複製でNDMFビルドと実アバターの見た目を確認します。異常があれば保存せず、バックアップへ戻します。

## 互換性の仕組み

Runtime／Editorの`.meta` GUIDとserialized field名を維持しています。旧MonoScriptを参照するScene／Prefabは同じGUIDの新版スクリプトへ解決されます。型名変更には`UnityEngine.Scripting.APIUpdating.MovedFrom`で旧namespace・assembly・class名を指定しました。

`Camellian.DistanceFade.*`と`Camellian.NonToonDistanceFade.*`のnamespace／assembly名は互換性のため維持しています。Shaderの`_DistanceFade`などはShader API名なので変更しません。フォルダー名変更時に`.meta`を削除してGUIDを再生成しないでください。

外部C#コードから旧型名を参照していた場合は新型名へ修正が必要です。NDMFの`QualifiedName`も新パッケージIDに変更しています。外部プラグインの`BeforePlugin`／`AfterPlugin`で旧IDを指定している場合は更新してください。

VPMの`legacyFolders`／`legacyPackages`による自動削除は設定していません。旧フォルダーに利用者の独自ファイルが入っている可能性があるため、この移行手順で内容を確認して退避してください。リポジトリ追加だけでは旧版は置き換わりません。
