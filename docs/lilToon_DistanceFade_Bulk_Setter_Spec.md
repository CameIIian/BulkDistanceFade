# lilToon Distance Fade Bulk Setter 仕様・設計書

文書バージョン: 1.1
作成日: 2026-09-08  
対象: VRChatアバター向け距離フェード自動設定ツールの初版  
原資料: [lilToon_DistanceFade_Bulk_Setter_Spec_Mock.docx](../lilToon_DistanceFade_Bulk_Setter_Spec_Mock.docx)

本書は原資料を実装・検証に使用できる仕様へ整理したものである。追加要件として、**設定コンポーネントはアバタールートに取り付けることを必須とする**。本書中の「初版の設計判断」は原資料の曖昧な点を具体化した決定であり、「実装時確認事項」は対応バージョンの実機検証を経て確定する項目である。本書の作成時点でUnity上の実装・動作検証は行っていない。

2026-09-09追記: Material除外リストは[マテリアル除外機能 設計書](Material_Exclusion_Spec.md)で追加仕様として定義し、実装した。除外に関する記述は追加仕様を優先する。

## 1. 目的と成果物

アバターの身体・衣装・髪などに使用されるlilToonマテリアルへ、共通の距離フェード設定をビルド時に自動適用する。利用者はアバタールートへ設定コンポーネントを1個追加し、Inspectorで値と適用項目を指定する。

NDMFによる非破壊処理を採用し、MA（Modular Avatar）とTTT（TexTransTool）が生成・置換したRenderer／Materialを処理後に走査する。AAO（Avatar Optimizer）の最適化前に設定を確定する。

開発成果物は、設定コンポーネント、専用Inspector、NDMFプラグイン、テスト、導入用READMEを含むVPMパッケージとする。本作業の成果物は、その開発に使用する本Markdown仕様・設計書である。

## 2. 環境・用語・対象範囲

### 2.1 想定環境

| 項目 | 要件 |
| --- | --- |
| Unity | 原資料に従い2022.3系を想定。具体的なパッチ版はVRChat SDKとの組合せで確定 |
| 必須環境 | VRChat Avatar SDK、lilToon、NDMF |
| 任意連携 | MA、TTT、AAO。未導入でもコンパイル・ビルドできること |
| 実行場所 | Unity Editor内のNDMFビルド処理 |
| 配布形式 | VPM Package |

対応バージョンの下限・上限と対象プラットフォームは、リリース前に互換性表へ記録する。想定環境を、すべてのバージョンやプラットフォームでの動作保証と解釈しない。

### 2.2 用語

| 用語 | 定義 |
| --- | --- |
| アバタールート | 編集時は対象の`VRC Avatar Descriptor`を持つGameObject。ビルド時はNDMFの`AvatarRootObject` |
| 元Material | 本ツールの適用パス開始時にRendererが参照しているMaterial。TTTなどの生成Materialも含む |
| 複製Material | 本ツールが元Materialから作成するビルド用Material |
| 対象Material | lilToon判定を満たし、適用ONの項目に対応するプロパティを1個以上持つMaterial |
| 適用ON／OFF | 各設定値を出力へ上書きするか、元Materialの値を保持するかの指定 |

NDMFのルート取得と一時アセット判定には、採用バージョンの`BuildContext` APIを使用する。[NDMF BuildContext API](https://ndmf.nadena.dev/api/nadena.dev.ndmf.BuildContext.html)

### 2.3 対象

- アバタールート自身および全子孫の`SkinnedMeshRenderer`と`MeshRenderer`。
- 非アクティブGameObject上、および`Renderer.enabled=false`の対象Renderer。
- 対象Rendererの`sharedMaterials`に含まれるlilToon系Material。
- MA／TTT等が適用パスまでに追加・置換したRendererとMaterial。

### 2.4 対象外

- Poiyomi、Standard等のlilToon以外のShader、ParticleSystemRenderer等の対象外Renderer。
- アバタールート外のRendererと、対象Rendererから参照されていないMaterial。
- AnimationClipのMaterialプロパティアニメーション、およびMaterial差し替えキーフレームの書き換え。
- MaterialPropertyBlock、Texture、Mesh、Shader、描画モード、Shaderキーワードの変更。
- 元Materialへの保存・上書き、Prefab Apply、生成Textureの恒久保存。
- 階層別設定、処理対象の手動Material指定、実行時の設定変更UI。Material除外リストは追加仕様で対応する。

アニメーションが同じプロパティを駆動する場合は再生中に値が上書きされ得る。また、AnimationClipだけから参照される差し替え先Materialは走査対象に含まれない。初版が保証するのは、適用パスで対象となったMaterialの静的初期値である。

## 3. コンポーネント配置・有効条件

### 3.1 必須配置

設定コンポーネント名は`DistanceFadeBulkSetter`、Inspector内の見出しは「BulkDistanceFade」とする。Add Componentのメニューは`BulkDistanceFade/Distance Fade Bulk Setter`とし、公式機能と誤認されない独立した分類を使用する（2026-09-09改訂）。

**`VRC Avatar Descriptor`と同じGameObject、すなわちアバタールートにのみ取り付ける。** 衣装、Armature、身体メッシュ、設定用の空GameObjectなど、子オブジェクトへの取り付けは許可しない。親にアバタールートが見つかっても、その子に付いた設定を採用しない。

```text
AvatarRoot                      ← VRC Avatar Descriptor
  ├─ DistanceFadeBulkSetter     ← 同じGameObjectのコンポーネント
  ├─ Body                       ← Renderer
  ├─ Hair                       ← Renderer
  └─ Outfit
       └─ Clothes               ← Renderer
```

この図の`DistanceFadeBulkSetter`は子GameObjectではなく、AvatarRootに付くコンポーネントを表す。対象範囲は常にアバター全体とし、範囲選択UIは設けない。

### 3.2 配置検証

初版の設計判断として、同一アバター内のコンポーネントは有効／無効を問わず合計1個までとする。`DisallowMultipleComponent`による同一GameObject内の重複防止に加え、アバター全体の検証を行う。

| 状態 | Inspector | ビルド時の扱い |
| --- | --- | --- |
| ルートに1個、有効 | 正常 | 設定を適用 |
| ルートに1個、無効 | 処理無効と表示 | MaterialとRendererを変更しない |
| コンポーネントなし | 該当なし | 何もしない |
| ルートの子に配置 | ルートへの付け直しを求めるError | 設定エラーとしてビルドを失敗させる |
| 同一アバター内に複数 | 重複箇所を示すError | 設定エラーとしてビルドを失敗させる |
| Descriptorのない場所に配置 | 有効なアバタールートではない旨を表示 | 他アバターの設定として使用しない |

配置検証は設定の有効／無効判定より先に行う。子にある無効コンポーネントも不正配置とする。ビルド中の他ツールによる移動で不正配置が隠れないよう、可能な最初の検証パスと適用直前の両方で確認する。最初の検証はMaterialを変更しない。

### 3.3 有効条件と寿命

- 処理ON／OFFの唯一の保存先は`Component.enabled`とし、別の重複フラグを持たない。初期値はON。
- 正しく配置されたコンポーネントは、ルートが非アクティブでもNDMFビルド対象として渡された場合に設定を適用する。`activeInHierarchy`を処理抑止条件にしない。
- コンポーネントは設定保持専用とし、`Update`等でMaterialを変更しない。
- 正常なビルド出力から設定コンポーネントを除去する。無効な設定でも除去し、元の編集用アバターには残す。
- ルートGameObjectへ`EditorOnly`タグを設定する方式は使用しない。アバター全体の除去を避け、設定コンポーネントだけを除去する。

## 4. 設定データと値の適用

### 4.1 設定項目

すべての適用Toggleの初期値はONとする。初期値は原資料の設定表を優先し、レイアウト例にある強度`1.00`は採用しない。

| 保存フィールド案 | Shader Property／要素 | 型 | 初期値 | Inspector表示 |
| --- | --- | --- | --- | --- |
| `fadeColor` | `_DistanceFadeColor` | HDR Color | `(0, 0, 0, 1)` | フェード色、Alphaあり |
| `startDistance` | `_DistanceFade.x` | float | `0.1` | 開始距離 |
| `endDistance` | `_DistanceFade.y` | float | `0.01` | 終了距離 |
| `strength` | `_DistanceFade.z` | float | `0` | 強度 |
| `backfaceShadow` | `_DistanceFade.w` | bool → float | `false` → `0` | 裏面を影にする（仮称） |
| `mode` | `_DistanceFadeMode` | int | `0` | モード |
| `rimColor` | `_DistanceFadeRimColor` | HDR Color | `(0, 0, 0, 0)` | リム色、Alphaあり |
| `rimFresnelPower` | `_DistanceFadeRimFresnelPower` | float | `5.0` | リムのフレネル指数 |

各行に対応する`override...`形式のboolを保存する。`rimColor.a`は`rimColor`の一部であり、独立した値や適用Toggleは持たない。Shader定義の初期値・型・フレネル指数のRangeは公式Shaderソースでも確認できる。[lilToon Shader定義](https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Shader/ltsmulti_o.shader)

強度`0`は実質的なフェード無効状態である。新規追加だけで見た目がフェードするとは説明せず、Inspectorに強度の調整が必要である旨を表示する。ただし適用ONであれば既存Materialの強度も`0`に上書きされる。

### 4.2 入力制約

- 適用ONの浮動小数点値とColorの全成分は有限値であること。NaN／Infinityは事前検証でErrorとする。
- 開始距離と終了距離を自動で入れ替えない。`開始 > 終了`も許可する。逆方向のフェードは有効な用途である。[lilToon 距離フェード](https://lilxyzw.github.io/lilToon/ja_JP/advanced/distancefade.html)
- 初版では開始／終了距離と強度に、原資料にない追加のハードClampを設けない。同距離を含む境界値の描画結果は対応Shaderで検証し、未検証の値を「推奨値」と表示しない。
- フレネル指数はInspectorで`0.01～50`へClampする。ビルド時にも適用ONなら同範囲へ正規化し、補正した場合はWarningを出す。NaN／InfinityはClampせずErrorとする。
- HDR ColorのRGB値を`0～1`へ丸めない。Alphaを含め保存値を適用する。
- Modeはintとして保持する。既知の値は対応バージョンの名称で表示し、未知の値は「Unknown (数値)」等で保持する。Inspectorを開いたことによる`0`への戻しは行わない。
- 適用OFFの保存値は書き込まず、値の不正によって今回の適用を止めない。配置不正は適用Toggleとは独立して検証する。

`_DistanceFade.w`の最終ラベルとModeの列挙名は、採用するlilToon版のEditor実装に照合して確定する。

### 4.3 部分上書き

`_DistanceFade`はVector4として一度読み取り、適用ONの要素だけを置き換え、`SetVector`で書き戻す。OFFの要素をコンポーネントの初期値で埋めてはならない。

```text
元Material:                  (0.4, 0.2, 0.7, 1)
開始距離だけ適用ON:           0.1
ビルド出力の_DistanceFade:    (0.1, 0.2, 0.7, 1)
```

Backface Shadowは適用ONのときだけ`false=0`、`true=1`として書く。OFFなら元のw値を、0／1以外であっても維持する。Color、Rim ColorはそれぞれRGBA全体で上書きする。

## 5. Inspector設計と利用手順

### 5.1 レイアウト

```text
BulkDistanceFade [コンポーネント有効チェック]
配置先: AvatarRoot
対象範囲: アバター全体（非アクティブを含む）
Strict lilToon Check [ON]

距離フェード
  [適用] フェード色         [HDR Color / Alpha]
  [適用] 開始距離           [0.10]
  [適用] 終了距離           [0.01]
  [適用] 強度               [0.00]
  [適用] 裏面を影にする     [OFF]
  [適用] モード             [Mode 0]
リム
  [適用] リム色             [HDR Color / Alpha]
  [適用] フレネル指数       [5.00]

編集時集計: 対象Renderer 14 / 対象Material 9（表示例）
[集計を更新]
※ MA・TTT処理後の実際の対象数とは異なる場合があります。
```

値の適用OFFでは対応入力欄を無効表示するが、保存値は消さない。処理全体が無効なときも設定値を保持する。設定の編集はUnityのUndo／RedoとPrefab overrideに対応する。

### 5.2 読み取り専用集計

集計は初回表示時と明示的な更新操作で実施する。設定変更等で古くなった集計には更新が必要なことを示し、毎描画でアバター全体を走査しない。

対象Renderer数は対象Materialを1個以上参照するRenderer数、対象Material数は参照同一性で重複排除した数とする。適用項目がすべてOFFなら対象数は0と表示する。必要に応じて走査Renderer総数を別記する。

この操作でNDMF変換を実行したり、Materialを複製・編集したり、SceneやAssetをDirtyにしてはならない。Inspector集計は編集時の参考値とし、ビルドパスで必ず再走査する。

### 5.3 利用手順

1. 必須パッケージと本ツールを導入する。
2. Hierarchyで`VRC Avatar Descriptor`のあるアバタールートを選択する。
3. Add Componentから`BulkDistanceFade/Distance Fade Bulk Setter`を1個追加する。
4. 適用項目、距離、強度、色等を指定する。強度の初期値は`0`であることを確認する。
5. Inspectorの配置・入力エラーを解消する。
6. NDMFが処理するPlay Modeまたはビルドで結果を確認する。

## 6. 対象Materialの判定

Shader名だけの曖昧な部分一致に依存せず、対応するlilToon版のShader名一覧と実在するプロパティを基に判定する。名前一覧には、検証済みの`Hidden/`等の公式バリアントを含める。任意のカスタムShaderが名前に「lilToon」を含むだけでは公式対応扱いにしない。

原資料のStrict設定は、初版では次のように具体化する。

- **ON（既定）:** 対応表にある公式Shader名を候補とし、各適用プロパティの存在を確認する。
- **OFF:** lilToon由来と判別できる名前のカスタムバリアントも候補とする。非lilToonの一般Shaderまで対象を広げない。
- ON／OFFのどちらでも、書き込み前の`Material.HasProperty`確認は必須とする。OFFは存在確認の省略を意味しない。

| 条件 | 処理 |
| --- | --- |
| `material == null` | 警告せずSkip |
| `material.shader == null` | 警告せずSkip |
| lilToon候補ではない | Skip |
| 適用ONのプロパティが存在する | 存在する項目だけ適用 |
| 適用ONのプロパティの一部が不足 | 不足項目をSkipし、Material単位でWarning |
| 適用ONのプロパティがすべて不足 | 複製せずSkipし、Material単位でWarning |
| 適用項目がすべてOFF | 複製・変更なし。プロパティ不足Warningも出さない |
| 同じMaterialを再検出 | キャッシュした複製を使用し、再適用しない |

`_DistanceFade`が存在しない場合はx／y／z／wの適用ON項目をまとめて非対応とする。Rimなど他の対応項目の適用は継続する。Warningは不足名をまとめ、同じ元Materialにつき1ビルド1回とする。

## 7. NDMF実行順

### 7.1 検証と適用の分離

設定位置・重複の初期検証は`BuildPhase.Resolving`の検証パスで行う。Material探索・複製・適用は`BuildPhase.Optimizing`で、TTTの後・AAOの前に行う。適用直前にも設定位置と値を再検証する。

実装時の修正（1.1）: TTT 1.0.1の実装ではTransformingだけでなくOptimizingでも設定適用とセッション終了処理を行うことを確認した。このため原案のTransformingでは「TTTの最終出力へ適用」を満たさない可能性がある。最終出力を優先する設計原則に従い、Optimizingへ移した。MA本体・late transformのTransforming処理はフェーズ順により完了済みとなり、同一フェーズ内でTTT後・AAO前を明示的に制約する。

適用パスの順序制約は次のとおりとする。MA本体・TTT相互の順序は本ツールで規定せず、それぞれの制約に従う。

| 関係 | Plugin QualifiedName | 根拠・位置づけ |
| --- | --- | --- |
| After | `nadena.dev.modular-avatar` | 原資料の必須順序 |
| After | `net.rs64.tex-trans-tool` | 原資料の必須順序 |
| After | `nadena.dev.modular-avatar.late-transform-stages` | MA後段を含める初版の設計判断 |
| Before | `com.anatawa12.avatar-optimizer` | 原資料の必須順序 |

NDMFの`BeforePlugin`／`AfterPlugin`は未導入プラグインを無視するため、名前による順序指定を用い、任意連携先の型を直接参照しない。[NDMF公式リポジトリ・実行順](https://github.com/bdunderscore/ndmf)

MA公式ソースには`nadena.dev.modular-avatar.late-transform-stages`が存在する。原資料の「必要なら後段制約を追加」を、初版では明示的なAfter制約として採用する。[MA PluginDefinition](https://github.com/bdunderscore/modular-avatar/blob/main/Editor/PluginDefinition/PluginDefinition.cs)

### 7.2 順序の保証条件

順序制約はフェーズごとのスケジューリングと組み合わせて評価する。`BeforePlugin`の記述だけでAAOの全フェーズより前になると解釈しない。採用バージョンの実行ログで、Materialを置換するMA／TTTの関連処理が適用前に終わり、Materialを最適化するAAOの関連処理が適用後であることを確認する。

制約の循環、Plugin名の変更、適用後のMaterial再置換が発見された組合せは、順序を修正・再検証するまで対応済みとしない。任意連携先がない構成も受入試験に含める。

### 7.3 Play ModeとPreview

NDMFによるPlay Mode移行時のアバター処理では、アップロード用ビルドと同じ適用ロジックを使用する。これは、NDMFのリアルタイムPreview拡張や、編集Sceneへの見た目の即時反映を実装することとは区別する。初版のInspector Previewは5.2節の集計のみとする。

## 8. 非破壊Material編集

### 8.1 不変条件

- 元Material、Prefab、Texture、Meshおよび編集用SceneのRenderer参照を変更しない。
- 変更対象はNDMFが処理するビルド用アバターと、本ツールの複製Materialのみとする。
- `Renderer.material`／`Renderer.materials`による暗黙の複製を使用しない。
- 読み出しと置換には`sharedMaterials`を使用し、配列長・Slot順・nullを保持する。
- 同一ビルド内では1元Materialにつき最大1複製とし、全対象Slotで共有する。
- キャッシュと警告済み集合はビルド単位に保持し、staticな状態で別アバターや次回ビルドへ持ち越さない。

### 8.2 複製と寿命

初版の設計判断として、**他プラグインが生成した一時Materialも、初回変更時に1回だけ複製する**。一時アセットであることだけでは本ツールが排他的に所有する根拠にならないため、原資料の安全側の複製方針を採用する。本ツール自身が生成・キャッシュした複製は再利用する。

複製には元Materialの設定全体を引き継ぎ、今回の適用ONかつ対応済みのプロパティだけを変更する。Shader、キーワード、Render Queue、Texture参照および無関係なプロパティは保持する。

生成物の保存・寿命はNDMFのアセット管理へ委ねる。`AssetContainer`／`AssetSaver`等の採用版APIに従い、アバターからの参照を正しく管理する。本ツール独自の`AssetDatabase.CreateAsset`／`SaveAssets`や固定出力フォルダーへの保存を行わない。[NDMF BuildContext・アセット管理](https://ndmf.nadena.dev/api/nadena.dev.ndmf.BuildContext.html)

「ビルド後にProjectへ残さない」は、本ツールがユーザー管理の恒久アセットを追加しないことを意味する。NDMFが通常ビルドや診断目的で管理する出力はNDMFのライフサイクルに従い、本ツールが独自に削除しない。

### 8.3 適用手順

1. 設定位置・個数を検証する。不正ならMaterialに触れる前にErrorでビルドを失敗させる。
2. 設定なしなら終了する。無効なら設定コンポーネントの除去だけを行う。
3. 適用ONの入力値をすべて検証し、書き込み用の設定スナップショットを確定する。
4. `AvatarRootObject.GetComponentsInChildren<Renderer>(true)`で再走査し、対象の2種類に絞る。
5. 全対象Slotを読み取り、Materialごとの判定結果・適用可能項目・置換先Slot一覧を作成する。
6. 変更項目がある元Materialだけを複製し、設定を適用してNDMF管理に載せる。
7. 複製と設定がすべて成功した後、変更のあるRendererだけに配列を再設定する。
8. ビルド用アバターの設定コンポーネントを除去し、集計を出力する。

途中で例外が発生した場合は失敗として報告し、不完全なビルド出力を成功扱いしない。入力不正時は複製・参照置換の開始前に止める。実装上の例外時も元Assetを変更しないことは保証する。

## 9. モジュール設計

| モジュール案 | 責務 |
| --- | --- |
| `DistanceFadeBulkSetter` | 設定値・適用Toggleのシリアライズと初期値定義 |
| `DistanceFadeBulkSetterEditor` | Inspector、入力補助、読み取り専用集計、Undo／Redo |
| `DistanceFadePlugin` | NDMF登録、検証・適用パス、実行順の宣言 |
| `ValidateSettingsPass` | 早期のルート配置・重複検証 |
| `ApplyDistanceFadePass` | 再検証、Material収集・計画・適用、設定コンポーネント除去 |
| `SettingsValidator` | 配置・入力検証、ビルド用設定の正規化 |
| `MaterialUtility` | Shader判定、HasProperty、Vector4部分更新 |
| `MaterialCloneCache` | ビルド内の複製共有、NDMFアセット管理との接続 |
| `BuildSummary` | 件数、Skip理由、重複排除したWarningの保持 |

Runtime側は設定コンポーネントの格納先であり、実行時処理を提供する意味ではない。`UnityEditor`とNDMF Editor APIへの参照はEditor assemblyへ限定する。MA／TTT／AAOへの必須assembly参照は追加しない。

## 10. エラーとログ

初版では「Error」を設定不正または処理失敗によるビルド失敗と定義する。NDMFの診断UIから問題のコンポーネント／Materialを特定できるようにする。

| ID | 事象 | 動作 |
| --- | --- | --- |
| E001 | ルート以外への配置 | Material変更前にビルド失敗 |
| E002 | 同一アバター内の複数配置 | 優先順位を付けずビルド失敗 |
| E003 | 適用ONの値がNaN／Infinity | Material変更前にビルド失敗 |
| E004 | 複製・適用・順序構築等の失敗 | 理由を記録しビルド失敗 |
| W001 | 適用ONのプロパティが不足 | 不足項目だけSkip、Materialごとに1回警告 |
| W002 | フレネル指数の範囲補正 | 補正後の値で続行、設定ごとに1回警告 |
| I001 | 対象Materialが0件 | 正常終了、集計のみ |
| I002 | コンポーネント無効／全項目OFF | Material変更なしで正常終了 |

正常時の集計には、走査Renderer数、対象Renderer数、重複排除した対象Material数、複製Material数、置換Slot数、Skip理由別件数を含める。対象なしや非lilToon混在だけでWarningを大量出力しない。

## 11. 非機能要件

- 走査とキャッシュによって処理量を対象Renderer／Slot数と一意Material数に比例させ、共有Materialの重複編集を避ける。
- 同じ入力から得られるプロパティ値と共有関係が、Hierarchy列挙順によって変化しないこと。
- ビルドの繰り返し、複数アバター、Play Mode終了後に参照やキャッシュが混入しないこと。
- Editor集計以外の自動ポーリングを行わず、実行時の継続処理を追加しないこと。
- プロパティの適用結果に加え、元Assetの不変性とNDMF管理下の生成物の寿命を検証すること。

## 12. パッケージ構成案

```text
Packages/com.example.liltoon-distance-fade/
  Runtime/
    DistanceFadeBulkSetter.cs
    DistanceFade.Runtime.asmdef
  Editor/
    DistanceFadeBulkSetterEditor.cs
    DistanceFadePlugin.cs
    ValidateSettingsPass.cs
    ApplyDistanceFadePass.cs
    SettingsValidator.cs
    MaterialUtility.cs
    MaterialCloneCache.cs
    BuildSummary.cs
    DistanceFade.Editor.asmdef
  Tests/
    Editor/
      SettingsValidatorTests.cs
      MaterialApplicationTests.cs
      NonDestructiveBuildTests.cs
      DistanceFade.Tests.Editor.asmdef
  package.json
  README.md
  CHANGELOG.md
  LICENSE
```

これは将来の実装配置案であり、現時点のリポジトリ構成ではない。`com.example.liltoon-distance-fade`は仮名とし、公開者の識別子へ変更する。`package.json`には採用版に対応したUnity条件、VPM依存関係、バージョンを記載する。READMEにはルートへの取り付け、初期強度0、非破壊動作、対応バージョン、アニメーションの制限を明記する。

## 13. 受入テスト

T01～T08は原資料の試験を引き継ぐ。追加要件と本書で具体化した境界条件をT09以降に追加する。以下は実装後の試験計画であり、実施済み結果ではない。

| ID | 条件・操作 | 期待結果 |
| --- | --- | --- |
| T01 | ルートに設定1個、lilToon Material1個 | 適用ONの全値が出力へ反映。元Assetは不変 |
| T02 | 同一Materialを3 Rendererと複数Slotで共有 | 複製は1個、すべての対象Slotが同じ複製を参照 |
| T03 | lilToonとStandardを混在 | lilToonのみ変更。Standardは値・参照とも不変 |
| T04 | MAで衣装を追加 | MAが追加したRenderer／Materialにも反映 |
| T05 | TTTでMaterialを差し替え | TTT出力を基に複製し、Texture等を保持して反映 |
| T06 | AAO併用 | 関連処理の順序をログで確認し、最終出力に設定が残る |
| T07 | NDMFのPlay Mode処理とアップロード用ビルド | 対象Materialの静的初期値が一致。元Assetは不変 |
| T08 | 正しく配置したコンポーネントを無効化 | MaterialとRenderer参照は不変、出力から設定のみ除去 |
| T09 | Descriptorと同じルートに配置 | 正常に認識し、ルート自身と全子孫を対象にする |
| T10 | 衣装や空の子GameObjectに設定を配置 | Inspector Error。無効設定でもビルド失敗。Material変更なし |
| T11 | ルート＋子、または不正データでルートに複数 | enabledの組合せによらず重複Error。後勝ち処理をしない |
| T12 | DescriptorのないGameObjectに設定を配置 | Inspectorで不正配置を表示。他アバターへ適用しない |
| T13 | 非アクティブ子、無効Renderer、非アクティブルート | ビルド対象として渡された範囲内なら有効設定を適用 |
| T14 | Vector4のxのみ適用ON | y／z／wを元Materialの値のまま維持 |
| T15 | 全適用ToggleをOFF | 複製0、参照置換0、プロパティ不足Warningなし |
| T16 | lilToon候補でRim等の一部プロパティ不足 | 対応項目だけ適用。不足名をまとめてMaterial単位1回Warning |
| T17 | null Material、null Shader、対象0件 | 例外・不要なWarningなし。配列長とSlot順を維持 |
| T18 | 適用ONのFloatまたはColorにNaN／Infinity | E003。複製・参照置換前に停止 |
| T19 | フレネル指数を0、負値、50超で保存 | 有限値は0.01～50に補正して適用。ビルド補正はWarning |
| T20 | 初期値、逆順距離、同距離、未知Mode、HDR Color | 強度初期値0。距離を並べ替えず未知値・HDR値を保持。描画境界も確認 |
| T21 | Inspector集計・Undo／Redo | 集計のみではAsset／Scene不変。設定編集はUndo／Redo可能 |
| T22 | MA／TTT／AAOの導入有無を全8構成で確認 | コンパイル成功。未導入Pluginの順序制約で失敗しない |
| T23 | 他ツールの一時Materialが複数Slotで共有 | 本ツールで1回だけ複製。元の一時Materialは不変 |
| T24 | 別アバターとMaterial共有、連続ビルド | 他アバターの参照・値は不変。ビルド間のキャッシュ混入なし |
| T25 | Materialプロパティアニメーション／差し替えClip | Clipは不変。静的初期値の適用と対象外の制限を確認 |
| T26 | Strict ON／OFF、公式Hidden Shader、類似名Shader | 対応表と候補判定に従う。どちらもHasProperty確認を行う |
| T27 | ビルド成功後の生成物・出力を確認 | 設定コンポーネントなし。独自の恒久Asset追加なし |
| T28 | 複製・適用中の失敗を発生させる | 成功扱いせず診断を記録。元Assetは不変 |

非破壊性は、ビルド前後の元Material／Prefab／Textureのファイルハッシュと、編集用Rendererの参照・Dirty状態を比較して確認する。共有関係はMaterial名ではなくオブジェクト参照で検証する。連携試験では使用した各パッケージ版とNDMF実行ログを記録する。

## 14. 原資料からの変更・実装時確認事項

### 14.1 本書で確定した変更と設計判断

| 原資料の箇所 | 本書での決定 |
| --- | --- |
| 3章: アバター配下の任意GameObjectに追加 | ユーザー追加要件によりアバタールート限定へ変更 |
| 5.1節: 複数配置はErrorまたは1個のみ許可を推奨 | 無効分も含め1アバター1個。不正配置・重複はビルドError |
| 3.1節と3.3節: 強度初期値0と表示例1.00が不一致 | 設定表の0を採用 |
| 3.2節: Toggleの初期値・Strictの詳細が未定義 | Toggleは全ON。Strictは候補範囲の厳格さとし、存在確認は常時必須 |
| 4.1節: MA late transformは必要に応じて追加 | late-transform-stagesにも明示的なAfter制約を追加 |
| 4.1節: Transformingで適用 | TTT 1.0.1の後段適用に対応し、1.1でOptimizingのTTT後・AAO前へ修正 |
| 4.2節: 既存Temporary Materialの再利用余地 | 他ツール由来は安全側に1回複製、本ツール内で共有 |
| 5.1節: NaN／Infinityで適用中止 | 事前検証でビルド失敗。変更開始前に停止 |
| 5.1節: Fresnelの下限Clamp | Shader Rangeに合わせ上限50も含め、ビルド時も正規化 |
| 記載なし: 設定コンポーネントの最終出力 | ビルド用アバターから除去し、編集元は保持 |

### 14.2 実装・リリース前に確認する項目

1. Unity、VRChat SDK、NDMF、lilToonの具体的な対応バージョンと対象プラットフォーム。
2. lilToon公式Shader名の対応表、カスタム候補の名前規則、wのUIラベル、Modeの列挙値・名称。
3. MA／TTT／AAOの採用版におけるQualifiedName、フェーズ、Material処理順と、早期配置検証の実行位置。
4. NDMFの採用版での一時アセット登録、設定コンポーネント除去、Errorによるビルド失敗の具体的なAPI。
5. Shaderバリアント・距離境界値・Shader機能の削減設定による実際の描画結果。HasPropertyだけで見た目の対応まで保証しない。
6. VPM公開用識別子、依存関係のバージョン範囲、ライセンス、配布先。

これらは仕様書作成を停止する項目ではないが、対応済みとしてパッケージを公開する前に解消する。外部リンクは2026-09-08に確認した資料であり、可変ブランチのソースを含むため、実装時には検証したタグまたはコミットも記録する。
