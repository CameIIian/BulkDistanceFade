# lilToon実装の参照資料と追加Shaderの共通設計

作成日: 2026-09-10  
基準: lazyFade 0.1.0 / 現在のRuntime・Editorコード  
状態: 第1～3章は実装の記録、第4～6章は追加Shader向けの設計要件

2026-09-10追記: NonToon対応はユーザー指定により別パッケージ・別コンポーネントで実装した。第4～6章の統合コンポーネント・共通除外案はNonToonには採用していない。現行構成は[NonToon設計の実装確定事項](lazyFade_NonToon_Spec.md)を優先する。lilToon用の保存データと動作は維持している。

## 1. この資料の役割

[ShaderCore＋NonToon設計](lazyFade_NonToon_Spec.md)と[Poiyomi設計](lazyFade_Poiyomi_Spec.md)から参照する。初期設計の31テスト時点ではなく、プリセット・Inspector整理・除外対応・Unitypackage作成まで完了した現在のlilToon実装を基準とする。追加Shaderへの対応コードはまだ存在しない。

[初版設計](lazyFade_lilToon_Spec.md)は判断の履歴、[除外設計](Material_Exclusion_Spec.md)は一致規則、[実装・検証記録](Implementation_Validation.md)は実行結果を確認するときに読む。最新の操作説明は[README](../README.md)を参照する。

## 2. 参考にする実装と責務

以下のパスは`Packages/com.camellian.lazyfade.liltoon/`内。

| 実装 | 現在の責務 | 追加Shaderでの利用方針 |
| --- | --- | --- |
| [Runtime/LazyFadeLilToon.cs](../Packages/com.camellian.lazyfade.liltoon/Runtime/LazyFadeLilToon.cs) | 保存データ、初期値、IEditorOnly、追加メニュー | 保存済みフィールド・型・GUIDを維持して対象別データを追加する |
| [Editor/SettingsValidator.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/SettingsValidator.cs) | ルート配置・重複・有限値検証、設定の取り込み | 配置・有限値検証は共有。OverridesとSnapshotはShader固有なので別型に分ける |
| [Editor/ApplyDistanceFadePass.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/ApplyDistanceFadePass.cs) | 読み取り専用Collectとビルド側Apply、除外、共有、参照確定 | 検出・計画・生成・保存・確定の制御へ拡張する |
| [Editor/MaterialUtility.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/MaterialUtility.cs) | Shader候補判定、Property対応と部分上書き | lilToon専用の意味を保持。別ShaderのPropertyへ単純置換しない |
| [Editor/MaterialCloneCache.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/MaterialCloneCache.cs) | 元Materialごとに1複製、保存、失敗時の複製破棄 | キャッシュの所有権を共有。Poiyomiには名前保持、生成対応には別Shaderキャッシュが必要 |
| [Editor/LazyFadeLilToonPlugin.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/LazyFadeLilToonPlugin.cs) | NDMF配置検証、適用順序、診断 | 既存順序を維持し、追加Shaderの生成・ロック順序を別途検証する |
| [Editor/LazyFadeLilToonEditor.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/LazyFadeLilToonEditor.cs) | 項目別チェック、基本設定、折りたたみ、集計 | 操作順とSerializedPropertyによる編集を引き継ぐ |
| [Editor/LazyFadeLilToonPresets.cs](../Packages/com.camellian.lazyfade.liltoon/Editor/LazyFadeLilToonPresets.cs) | プリセット定義と検証後の即時値コピー | 名前・値の編集箇所と反映処理を分離する構成を引き継ぐ |
| [Tests/Editor/LazyFadeLilToonTests.cs](../Packages/com.camellian.lazyfade.liltoon/Tests/Editor/LazyFadeLilToonTests.cs) | 非破壊性、除外、互換性、Inspector、プリセットの回帰テスト | 61件を基準に維持し、各対象の実Shaderテストを追加する |

## 3. 現行の動作と、引き継いではいけない仮定

### 3.1 現在の設定

アバター全体で、VRC Avatar Descriptorと同じGameObjectにコンポーネントを1個配置する。無効なコンポーネントも配置・重複検証の対象。Add Componentの表示は`lazyFade/lazyFade lilToon`で、Shader公式機能と誤認させる命名を避けている。

| lilToonの基本項目 | 現在の新規追加時の値 | 適用チェック |
| --- | --- | --- |
| 開始距離 / 終了距離 | 0.18 / 0.01 | ON / ON |
| フェード色 | RGB #0A0707、Alpha 1 | ON |
| 強度 | 0.95 | ON |
| リム色 | RGB #FFBCB1、Alpha 0 | ON |
| リムライトの細さ | 4.5 | ON |
| 裏面を陰にする / モード | false / 頂点（0） | OFF / OFF |

StrictはON、除外リストは空。lilToonの`_DistanceFade`はx=開始、y=終了、z=強度、w=裏面である。この要素割り当て、開始が終了より大きい初期値、色・リムの意味を追加Shaderへ流用してはならない。色はColor32から正規化し、任意のHDR保存値は勝手に変換しない。

### 3.2 Inspectorとプリセット

上から「1. 処理の有効化」「2. 基本設定」「3. 集計・確認」。基本設定の距離フェード4項目とリム2項目は常時表示し、リムをオプションへ戻さない。各行の適用は文字ラベルなしのチェックボックス、説明はツールチップに置く。

除外Materialは配列そのものを1段の折りたたみリストとして表示する。標準配列GUIをBeginFoldoutHeaderGroupで囲わない。高度な設定には裏面・モード・Strictを置く。

`warm`・`cold`・`none`は基本設定の先頭で選択すると6値を即時上書きする。OFF項目の保存値も更新するが、適用チェック・全体有効状態・除外・高度な設定は保持する。選択名はInspectorの表示状態だけで、保存するのは実値。専用の読み込み・確定・Undo/Redoボタンは設けない。SerializedObjectの通常の保存・Prefab override経路は維持する。基本項目の手動編集時は個別指定へ戻す。

追加Shaderでもこの操作規則を採用するが、6項目という数や`warm/cold/none`の値は共通仕様にしない。

### 3.3 走査・非破壊性・検証実績

MeshRenderer／SkinnedMeshRendererを非アクティブ・無効も含めて走査し、sharedMaterialsの順序とnullを保持する。除外はShader判定より前にMaterial参照とNDMF登録済み起源で判定し、元配列やObjectRegistryを書き換えない。名前だけで除外を引き継がない。

元Materialごとに1複製を生成し、保存成功後にRenderer参照を変更する。参照変更に失敗したら本処理で変更した参照を戻す。元アセットは変更せず、処理後のビルド用設定コンポーネントを除去する。現在の複製名には`(Distance Fade)`を付けるが、名前が生成Propertyへ影響する追加Shaderにはそのまま適用しない。

「再集計」は編集時の読み取り専用確認。未更新でもNDMFでは最新の設定を再取得する。コンポーネント無効時や全項目OFFは対象0件となり、全項目OFFは除外件数より優先される。除外数は登録要素数ではなく、走査中に遭遇した一意Material数とSlot数。

既存Edit Modeは61件合格。Unitypackageでは再インポート後のコンパイルと37ファイルの内容一致を確認済み。これらは追加Shaderや実アバターでの描画検証の代わりにはならない。

## 4. 追加Shaderの共通構成案

本設計では既存のルートコンポーネント1個を維持し、対象別データとEditor側のアダプターを追加する。Shaderごとに同じMaterialを別コンポーネントが上書きする構成にはしない。独立配布が必要なら接続部分を任意のアドオンassemblyに分離する。

新しい`enableNonToon`／`enablePoiyomi`と全適用チェックは初期OFF。旧データで追加ブロックがnullの場合も無効として取り込み、Inspector表示だけで保存データを移行しない。Runtimeデータには外部Editorの型を含めず、追加Shader未導入でも既存lilToonだけでコンパイルできることを必須にする。既存lilToonのStrict判定は追加対象へ転用しない。

共通の計画データは、元Material、対象ID、対応版、変更項目、保持値と合成した最終値、名前保持方針、生成／ロック要否、失敗理由を持つ。`Match`・`Plan`・集計は読み取り専用、複製への書き込みはビルド時だけにする。具体的な型名やインターフェースは未実装である。

一つのMaterialは一つの対象だけが担当する。判定競合は常にエラー。Materialキャッシュは元参照・対象・計画を区別し、ビルド間で共有しない。Shader生成キャッシュは元Shader・モジュール構成・生成入力・対応版・ビルドターゲットで分ける。

## 5. 適用手順・診断・UI

1. Resolvingで配置を検証する。適用直前にも再検証し、ON項目の非有限値を検出する。
2. 設定を固定して最終Renderer／Materialを走査し、共通除外を適用する。
3. 対象別に判定し、保持値と合成した最終値・機能依存・対応状態を全Materialで計画する。
4. エラーがなければ必要な複製だけを作る。対応済みの生成・ロック処理を複製側で行う。
5. 生成後のProperty名と値を検証し、NDMF管理下へ保存する。すべて成功してからRenderer参照を確定する。
6. 設定をビルド出力から除去し、適用・部分適用・スキップ・除外を報告する。

現在のOptimizing段階はMA本体・MA late-transform・TTTの後、AAOの前を指定している。この宣言を実際の外部Pluginの実行段階と混同しない。追加Shaderの生成やSDKコールバックのロックがこの順序と両立するか、採用版のビルドログで確認する。未知のPlugin IDやAPIを推測してハードコードしない。

元Material・Shader・Texture・Scene・Prefab・AnimationClip・Importer設定を変更しない。将来の生成対応で許すShader／キーワード変更は、明示的に要求した機能に必要なビルド用複製上だけ。生成物の保存・後始末まで成立する経路だけを提供する。

| 状況 | 提案する扱い |
| --- | --- |
| 全体／対象OFF、全適用OFF、手動除外 | 複製・生成・ロックなし。通常の対象外は警告しない |
| ON入力の非有限値、最終距離ペアの不正、判定競合 | 書き込み開始前にビルドエラー |
| 未対応版、出自不明、モジュール不足、復元不能ロック | 既定WarnAndSkipでMaterial全体を警告してスキップ。FailBuild選択時は停止 |
| 独立した任意項目のみ非対応 | 項目をスキップしてMaterial単位で警告。機能全体の成立条件を損なう場合は全体をスキップ |
| 生成・ロック・保存・参照確定で実行時失敗 | 常に失敗。元データを保持し、変更したビルド参照を復旧 |

UIは既存lilToon基本設定の常時表示を維持し、その下に追加Shaderの有効化と対象別設定を表示する案とする。有効な対象の基本項目は上から入力でき、任意・高度な機能だけ折りたたむ。共通除外は一つだけ設ける。未導入・未対応は理由を表示し、実装のない生成ボタンを操作可能にしない。

集計はShader別件数と全体件数を区別する。一つのRendererが複数Shaderを持つため、全体Renderer数はShader別件数の単純合計にしない。集計中に生成・解除・ロック・出自の登録・Material変更を行わない。

## 6. 共通受入条件

| ID | 試験と期待結果 |
| --- | --- |
| C01 | 既存61件の意味を維持し、旧Scene／Prefabの値・型・GUID・チェック状態を保持 |
| C02 | 新規ブロックなし／null／OFFと追加Shader未導入で既存lilToonが従来どおり動作 |
| C03 | 混在Materialで対象の誤判定なし。同名Property、同名Material、判定競合を試験 |
| C04 | 除外・置換起源・重複・null・全項目OFF・非アクティブを現行規則で処理 |
| C05 | 共有Materialは1複製、元アセットのバイト・Dirty状態・参照・キーワードが不変 |
| C06 | 連続ビルド・別アバターで生成物とキャッシュが混入しない |
| C07 | 入力不正・生成失敗・保存失敗・参照確定失敗で部分成功を報告しない |
| C08 | 除外リストと高度設定を同時展開してGUIエラーなし。集計は変更なし、未更新でも最新値適用 |
| C09 | 対象別プリセットがその対象の値だけを即時更新し、チェック・他対象・除外を保持 |
| C10 | MA／TTT／AAOと対象Shaderの生成・ロックを含む最終出力、VR・鏡・カメラ・Play Modeを実機確認 |

追加対象の対応版と導入なし構成をCI／ローカルで別々に試験する。Shader本体やSDKは配布物へ無断同梱せず、実装済み・未検証・対象外をREADMEとリリース本文で区別する。
