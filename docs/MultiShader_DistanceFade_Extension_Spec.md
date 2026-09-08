# 距離フェードのマルチShader拡張仕様書

文書バージョン: 1.0  
作成日: 2026-09-08  
対象: PoiyomiおよびShaderCore＋NonToonへの将来拡張  
状態: 拡張設計。追加Shaderへの対応実装・動作検証は未実施

## 1. 目的と前提

現在のlilToon専用ツールを、lilToon・Poiyomi・ShaderCore上のNonToonが混在するアバターで使用できるようにする。Shader固有の設定・生成・最適化をアダプターへ分離し、Renderer走査、非破壊複製、設定検証、集計を共通化する。

本書は[既存仕様・設計書1.1](lilToon_DistanceFade_Bulk_Setter_Spec.md)と[実装・検証記録](Implementation_Validation.md)に対する別冊である。現行パッケージは`com.camellian.liltoon-distance-fade` 0.1.0であり、本書を追加した時点ではlilToon専用の動作を維持する。

**設定コンポーネントは引き続きアバタールート、すなわちVRC Avatar Descriptorと同じGameObjectへ1個だけ取り付ける。** Shaderごとにコンポーネントを追加する方式は採用しない。

本書では次を区別する。

- **確認済み事項:** 現行コード、導入済みパッケージのソース、公式資料から確認した構造・値。
- **拡張要件:** 今後の実装が満たすべき動作。
- **実装時確認事項:** API、Shader生成、最適化順序等について実機検証が必要な条件。

## 2. 対象と段階

### 2.1 拡張対象

| Shader系統 | 初期対応の対象機能 | 初期実装の制限 |
| --- | --- | --- |
| lilToon | 現行Distance Fade設定一式 | 現行の値・初期値・部分上書きを維持 |
| Poiyomi Toon | Proximity Colorによる距離依存の色変更 | 検証したバージョンとロック処理経路のみ |
| ShaderCore＋NonToon | NonToonのDistance Fadeモジュール | 検証したCore／NonToon／モジュール構成のみ |

ShaderCoreはShaderを構成する基盤、NonToonはその基盤上のShaderとして扱う。ShaderCore製のShaderをすべてNonToonと見なさない。NonToonは公式にも開発中とされているため、対応範囲はバージョンの組合せで管理する。[Shader Core公式](https://github.com/lilxyzw/Shader-Core)、[NonToon公式](https://github.com/lilxyzw/NonToon)

### 2.2 初期拡張の対象外

- Poiyomi Pro、未検証のカスタム派生Shaderへの自動適用。
- 透明度フェード、Dissolve、輪郭線だけの距離フェードへの自動変換。
- Shader間で描画結果が完全一致することの保証。
- AnimationClip、MaterialPropertyBlock、元Prefabの書き換え。
- 全Shader共通の任意Property編集UI、Materialの手動除外、階層別設定。
- 元Shaderのロック解除、元`.scshader`のモジュール設定変更、元Shaderの再インポート。

### 2.3 実装順

1. **基盤共通化:** lilToonの処理をアダプターへ移し、既存31件のテストを維持する。
2. **NonToon既存モジュール対応:** Distance Fadeを含む生成済みShaderへの設定を実装する。
3. **Poiyomi対応:** Proximity Colorの値設定と、ロック前後の有効性を検証して提供する。
4. **NonToonモジュール自動追加:** ビルド用Shader生成が非破壊で成立する版に限定して追加する。

2と3の着手順は検証環境により入れ替えてよい。4の完成を、既存モジュールへの値設定の提供条件にはしない。

## 3. 調査基準と意味の違い

### 3.1 今回ソースを確認した版

| 対象 | 基準 | 確認内容 |
| --- | --- | --- |
| 現行ツール | 0.1.0 | `MaterialUtility`、`SettingsSnapshot`、ルート検証、NDMF適用 |
| Poiyomi Toon | ローカル導入版9.3.64 | Proximity ColorのProperty宣言・描画処理 |
| ShaderCore | ローカル導入版0.1.11 | パッケージID、モジュール識別・Property生成の構造 |
| NonToon | ローカル導入版0.1.3 | Distance FadeモジュールID、Property宣言・計算式 |
| Poiyomi公式Web資料 | 掲載版10.0 | Proximity Colorの用途、ロック・アニメーションの制約 |

ソース確認は動作保証ではない。Poiyomi 9.3.64のProperty表を、公式Web資料10.0だけを根拠に10.0へ適用してはならない。実装時には検証した版、ソースのタグ／コミットまたはハッシュ、NDMF版、描画結果を対応表に記録する。

### 3.2 同名・類似機能を一律に変換しない

| 観点 | lilToon | Poiyomi Proximity Color | NonToon Distance Fade |
| --- | --- | --- | --- |
| 主な結果 | フェード色等への変化 | 距離に応じたRGB乗算 | 黒方向への減光 |
| 距離入力 | 開始・終了距離 | Min・Max Distance | Vectorのx・yで範囲指定 |
| 強度 | `_DistanceFade.z` | 確認した機能に独立した強度Propertyなし | 独立した`_DistanceFadeStrength` |
| 距離基準 | lilToon固有Mode | 0=Object、1=Pixel（9.3.64） | 確認した処理は`vertex.headDepth` |
| 色・Alpha | 現行のHDR Colorを維持 | 確認した描画処理はRGBを使用 | 確認したモジュールに色Propertyなし |
| リム関連 | 現行設定あり | 本機能の対応先なし | 本モジュールの対応先なし |

Poiyomi公式はProximity Colorをカメラ距離による色変更として説明し、近距離・遠距離でそれぞれベース色に乗算する色を指定する。単なるlilToonのフェード色コピーでは同じ意味にならない。[Poiyomi Proximity Color](https://www.poiyomi.com/special-fx/proximity-color)

共通化するのは実行基盤と設定操作の形式であり、ネイティブな設定値はShaderごとに保持する。初期拡張では「全Shaderへ同じ数値をコピー」ボタンを実装しない。将来の共通プリセットは、結果の差と変換不能項目を表示する独立機能とする。

## 4. 設定モデルと既存データ互換性

### 4.1 ルートコンポーネント

`DistanceFadeBulkSetter`のクラス名、namespace、Runtime assembly名、スクリプトGUIDを維持する。既存Scene／Prefab内の参照を壊す名称変更やファイル再作成を行わない。表示名は必要に応じて「Distance Fade Bulk Setter」へ変更できる。

既存のlilToon用フィールドと`override...`はそのまま読み込む。新しいShader別ブロックを追加し、各ブロックは純粋なシリアライズ可能データで構成する。Runtime assemblyからPoiyomi EditorやShaderCore Editorの型を参照しない。

| 設定ブロック案 | 初期状態 | 動作 |
| --- | --- | --- |
| 既存lilToon設定＋`enableLilToon` | ON | 旧設定をそのまま適用 |
| `poiyomiSettings`＋`enablePoiyomi` | OFF | 明示的にONにした場合のみ対象化 |
| `nonToonSettings`＋`enableNonToon` | OFF | 明示的にONにした場合のみ対象化 |
| `schemaVersion` | 旧データを識別可能な版番号 | 読み込み互換性と移行管理 |
| `unsupportedMaterialPolicy` | `WarnAndSkip` | 未対応Materialを警告して除外 |

全体ON／OFFは引き続き`Component.enabled`だけを使用する。新規Shaderの追加・有効化によって、既存lilToonフィールドの初期値や適用Toggleを変更しない。

### 4.2 移行規則

- 旧schemaは読み取り時に互換用スナップショットへ変換し、lilToon ON、新規Shader OFFとして解釈する。
- 新規シリアライズフィールドのデフォルト動作だけに依存せず、旧Prefab／Sceneを実際に読み込む回帰試験を行う。
- Inspectorを開いたことやNDMFビルドだけを理由に、元Scene／Prefabへ移行結果を保存しない。
- 必要な永続移行は明示操作とし、Undo／Redo、Prefab override、無効コンポーネントを含めて扱う。
- パッケージIDは初期拡張では維持する。共通基盤の別パッケージ化やlilToon必須依存の解除は、別の移行版で行う。

## 5. Poiyomiアダプター仕様

### 5.1 Property対応

以下はPoiyomi Toon 9.3.64に含まれる`_PoiyomiShaders/Shaders/9.0/Toon/Poiyomi Toon Early Outline.shader`で確認した候補マッピングである。対象Shader一覧と他バリアントでの一致は実装時に検証する。

| 設定名案 | Property | 保存型 | 拡張設定の初期値案 |
| --- | --- | --- | --- |
| `effectEnabled` | `_FXProximityColor` | bool→float | false |
| `positionMode` | `_FXProximityColorType` | int | 1（Pixel） |
| `minColor` | `_FXProximityColorMinColor` | Color、描画はRGB | black |
| `maxColor` | `_FXProximityColorMaxColor` | Color、描画はRGB | white |
| `minDistance` | `_FXProximityColorMinDistance` | float | 0 |
| `maxDistance` | `_FXProximityColorMaxDistance` | float | 1 |
| `forceBackfaceColor` | `_FXProximityColorBackFace` | bool→float | false |

各項目に独立した適用Toggleを設け、**新規PoiyomiブロックのToggle初期値は全OFF**とする。アダプターONだけでは既存値を上書きしない。機能の有効化Propertyを適用する場合は、依存する機能状態も含めて計画・検証する。

Min／Max ColorのTheme Indexもソース上に存在する。初期拡張ではTheme Indexを勝手に0へ戻さない。Theme設定によって指定RGBが使用されない構成は「値が見た目に反映されない可能性」を表示し、対象版で解決方法を検証するまで色適用の保証範囲から除外する。

### 5.2 値の検証

- 適用ONのfloatと使用Color成分は有限値であること。
- MinとMaxの一部だけを上書きする場合も、元Materialから残す値と合成した**最終ペア**を検証する。
- 初期対応では距離ペアを`0 <= Min < Max`に限定する。同値による計算の不定性を避け、逆順は色の役割を反転して表現する。自動並べ替えはしない。
- Modeの未知値はシリアライズ上保持するが、明示適用時には対象版で対応が確認できなければ未対応扱いとする。
- ColorのAlphaを透明度フェードとして説明しない。HDR値の対応範囲はShader宣言と描画を照合して決める。
- Backface ColorはCullの状態に依存するため、Cullが有効な場合に注意を表示する。Cull、Render Queue、描画モードは変更しない。[Poiyomi Proximity Color・BackFace条件](https://www.poiyomi.com/special-fx/proximity-color)

### 5.3 ロック対応

Poiyomiではロック時に未使用機能や静的Propertyが最適化され、アップロード時の自動ロックも存在する。`HasProperty`と`SetFloat`の成功だけで描画への反映を判定しない。[Poiyomi Locking and Animation](https://www.poiyomi.com/general/locking)

| 検出状態 | 拡張要件 |
| --- | --- |
| 未ロック、対応版 | 複製へ設定し、その値を反映した最終ロックまで検証 |
| ロック済み、復元情報と非破壊再ロック経路が利用可能 | ビルド用複製だけを復元・設定・再ロック |
| ロック済み、必要機能が除去済み／復元不可 | Material全体を未対応扱い。元Shaderを変更しない |
| 動的Propertyだけが残る | 機能の生存・Property名を確認できた項目だけ対応可能とする |
| 状態判別不能 | 名前による推測で編集せず未対応扱い |

初期リリースで非破壊再ロックが未完成なら、その構成は明示的に非対応とする。黙って適用成功と報告しない。

現在の複製名には`(Distance Fade)`が付くが、PoiyomiのRenamed Animated PropertyではMaterial名が生成名に関係する。Poiyomiアダプターは元の名前・リネーム情報を保持し、既存アニメーション名を変えない。アニメーション用のフラグも勝手に付け直さない。維持できないロック構成は除外する。[Poiyomi・Renamed Property](https://www.poiyomi.com/general/locking)

## 6. ShaderCore＋NonToonアダプター仕様

### 6.1 対象の識別

Shader名の`NonToon`部分一致だけでは判定しない。元`.scshader`または生成元メタデータ、NonToon由来の情報、ShaderCore版、選択済みモジュールを確認する。他ツール生成のShaderも、出自と構成を確認できる場合のみ対象とする。

初期対象モジュールIDは`jp.lilxyzw.nontoon.distancefade`。ローカルNonToon 0.1.3の`Shaders/Modules/DistanceFade/jp.lilxyzw.nontoon.distancefade.scmodule`で確認した。モジュールがインストール済みであることと、対象Shaderに組み込まれていることを区別する。

### 6.2 Propertyと描画意味

| 設定名案 | モジュール内の宣言名 | 型・要素 | 初期値 |
| --- | --- | --- | --- |
| `nearDistance` | `_DistanceFade` | Vector4.x | 0.01 |
| `farDistance` | `_DistanceFade` | Vector4.y | 0.1 |
| `strength` | `_DistanceFadeStrength` | float | 0 |

NonToonの宣言は距離Vectorと独立した強度で構成される。lilToonと同じ`_DistanceFade`という宣言名でもzへ強度を書いてはならない。x／yの適用Toggleを別々に持ち、z／wは保持する。[NonToon Property宣言](https://raw.githubusercontent.com/lilxyzw/NonToon/main/Shaders/Modules/DistanceFade/properties.hlsl)

確認した実装は`vertex.headDepth`を用い、近距離側で強度に応じて黒へ減光する。色、リム、裏面影、Modeの対応Propertyはこのモジュールには確認できていない。カメラからのユークリッド距離と同一であるとは説明せず、headDepthの生成とVR／鏡の挙動を検証する。[NonToon距離フェード処理](https://raw.githubusercontent.com/lilxyzw/NonToon/main/Shaders/Modules/DistanceFade/phase_postpixel.hlsl)

ShaderCoreはモジュールIDを使って変数名の衝突を避ける。表中の名前は**モジュール内の宣言名**であり、Materialへ渡す最終Property名とは限らない。対応版APIまたは生成情報から最終名を解決し、型と存在を検証する。文字列連結で命名規則を推測しない。[Shader Coreの機能](https://github.com/lilxyzw/Shader-Core)

### 6.3 値とモジュールの扱い

- 新規NonToonブロックはOFF、各適用Toggleも全OFFとする。
- 初期対応では距離の最終ペアを`0 <= near < far <= 1`、強度を`0～1`に限定する。これは確認したUI属性に合わせた提供範囲であり、Shaderの物理的な上限という意味ではない。
- 範囲外・非有限値はErrorとし、距離の並べ替えや無言のClampをしない。
- モジュール組み込み済みなら、ビルド用Materialへ解決済みProperty名で適用する。
- モジュール未組み込みは既定で警告してSkipする。Stage 4の「不足モジュールをビルド時に追加」を明示ONにした場合のみ生成経路へ進む。
- 非破壊生成を実装する際は、元Shaderと選択済みモジュールを基にビルド専用Shaderを生成する。既存モジュール、順序、設定を保持し、Distance Fadeだけを追加する。
- 生成後にProperty一覧を再解決する。生成前の名前・IDキャッシュを流用しない。
- 定数化属性やローカルキーワードを持つPropertyは、対象版の規則に従って複製上で同期する。無関係なキーワードは保持する。

ShaderCore公式は`SCConstValue`と生成キーワードによる定数化を説明している。Material値だけでは設定が確定しない可能性をアダプターで扱う。[Shader Core・定数化](https://github.com/lilxyzw/Shader-Core)

## 7. 共通アーキテクチャ

### 7.1 現行コードからの責務分離

| 現行 | 拡張後の役割 |
| --- | --- |
| `MaterialUtility` | lilToon固有部分を`LilToonDistanceFadeAdapter`へ移動 |
| `OfficialShaders` | lilToonアダプター専用の識別データとして維持 |
| `Overrides`／`SettingsSnapshot` | lilToon用として維持し、Poiyomi／NonToonに専用スナップショットを追加 |
| `SettingsValidator` | 共通配置検証とShader別の設定・最終Material値検証を分離 |
| `ApplyDistanceFadePass` | 検出、計画、生成、適用、確定を制御する共通処理 |
| `MaterialCloneCache` | Material複製キャッシュ。Shader生成キャッシュは別管理 |
| `BuildSummary`／`BuildDiagnostic` | Shader別集計、未対応理由、変換・ロック結果を追加 |
| `DistanceFadeBulkSetterEditor` | ルート配置と全体設定に加え、Shader別Foldoutを表示 |

### 7.2 アダプター契約

以下は設計上のインターフェース案であり、現在提供されているAPIではない。

```csharp
interface IDistanceFadeAdapter
{
    string Id { get; }
    MatchResult Match(Material source, InstalledShaderInfo environment);
    AdapterCapabilities Describe(Material source);
    MaterialChangePlan Plan(Material source, AdapterSettingsSnapshot settings);
    void ApplyToClone(Material destination, MaterialChangePlan plan, BuildServices services);
    VerificationResult Verify(Material output, MaterialChangePlan plan);
}
```

`Match`、`Describe`、`Plan`は読み取り専用とする。`Plan`は最終値、変更Property、機能依存関係、生成／ロック要否、名前保持方針、診断を返す。アダプターがRenderer参照や元Assetへ直接書き込むことは禁止する。

能力情報は少なくとも、項目単位の対応、Color／Alphaの意味、距離基準、機能有効化、Shader生成、ロック処理、未知値の対応可否を持つ。`HasProperty`は能力判定の一部であり、判定全体の代わりにしない。

### 7.3 識別と共有

- 1つのMaterialは1つのアダプターだけが処理する。複数候補が一致した場合は順序で選ばずErrorとする。
- lilToonのStrict ON／OFFの意味は維持する。Poiyomi／NonToonの判定へそのまま転用しない。
- 同一Materialを参照する全対象Slotに同じ計画を適用し、1個の複製を共有する。
- キャッシュキーは元Materialの参照、アダプターID、設定の識別子とする。異なる計画が同じ元Materialへ発生した場合は構成競合として検出する。
- Shader生成キャッシュは元Shader、選択モジュール構成、定数化等の生成入力、対象版、ビルドターゲットで区別する。
- キーワード・ロック由来の最終Property名・Material名も意味のある状態として計画に含める。
- すべての可変キャッシュはビルド単位とし、別アバターや次回ビルドへ引き継がない。

## 8. 非破壊処理と実行順

### 8.1 ビルド手順

1. Resolvingでルート配置・重複を検証する。
2. 適用時に再検証し、有効なShader別設定のスナップショットを作る。
3. MA／TTT等の出力から対象RendererとMaterialを再走査する。
4. 全Materialのアダプター判定、対応確認、最終値検証を完了する。
5. 対応する計画だけに対してMaterialを複製する。必要なShader生成・機能同期を行う。
6. 設定適用後、Property値だけでなく生成・ロック結果の有効性も確認する。
7. 生成物をNDMF管理に載せ、成功した計画のRenderer参照を確定する。
8. ビルド用設定コンポーネントを除去し、Shader別集計を出力する。

入力Errorは書き込み開始前に止める。生成中・ロック中の失敗はビルド失敗とし、元Assetを保持する。Renderer参照を確定する前に処理を完了させ、確定中の例外では本ツールが置換したビルド用参照を戻す。

### 8.2 順序制約

基準位置は現行と同じ**Optimizing内のTTT後・AAO前**とする。MA本体・late transformは前段のTransformingで完了する。TTTのOptimizing処理後を待つ設計を維持する。

追加Shaderでは次の相対関係も満たす必要がある。

```text
Material／モジュール構成を決める連携処理
  → 最終Materialの再走査
  → 距離フェード設定・必要なShader再生成
  → 設定を焼き込むロック／定数化
  → 設定を前提とする最適化
```

Poiyomiの自動ロックやShaderCore連携ツールがNDMF外のSDKコールバックを使う場合も、実行経路全体を確認する。未確認のPlugin QualifiedNameを推測して制約へ追加しない。TTT後・AAO前と追加Shaderのロック順序が両立しない版は対応外とし、必要なら段階を分けた統合処理を設計する。

ShaderCore Module Override等の外部ツールが生成したShaderを受け取る場合は、出自とモジュール情報を引き継げることを確認する。特定の外部ツールを自動的に必須依存へ追加しない。

### 8.3 非破壊の境界

元Material、Shader、Texture、`.scshader`、`.scmodule`、Importer設定、元PrefabとSceneは変更しない。生成したShaderとMaterialだけをNDMFの管理対象とし、独自の恒久保存や元Shaderの再インポートを行わない。

現行の「ShaderキーワードやShaderを変更しない」原則に対し、将来の生成対応では**ビルド用複製上で、要求した機能に必要な変更だけを許可する**例外を設ける。例外はPoiyomiの再ロック、NonToonのモジュール追加・定数化同期に限定し、Inspectorと診断で操作内容を表示する。

## 9. Inspectorと診断

### 9.1 UI

全体の有効化、配置先、対象範囲の下に、lilToon／Poiyomi／ShaderCore＋NonToonのFoldoutを置く。各ブロックで処理ON／OFF、導入版、対応状態、ネイティブ設定、項目別適用Toggleを表示する。

- 未導入Shaderの設定値も保存できるが、処理不可の状態を明示する。
- 他Shaderの値から自動コピーしない。描画意味が異なる項目にはそのShaderの名称を使う。
- 未対応項目は編集欄を無効化して理由を表示する。未知の保存値は破棄しない。
- 集計は読み取り専用のままとし、Shader生成、ロック解除、プレビュー描画変更を起こさない。
- Shader別に対象Renderer／一意Material／対応可能／モジュール不足／ロックで適用不可の件数を出す。
- 全体のRenderer数は重複排除する。複数Shaderを持つRendererがあるため、Shader別件数の単純合計を全体件数としない。

### 9.2 失敗ポリシー

`WarnAndSkip`では、対応版不明、必要モジュールなし、ロック復元不可等のMaterialを丸ごと除外して警告する。`FailBuild`では同じ状態をErrorとする。通常の非対象ShaderやOFFのブロックは警告対象にしない。

| 事象 | 扱い |
| --- | --- |
| 既存の配置・重複・非有限値エラー | 現行Errorを維持 |
| 複数アダプターが同じMaterialに一致 | 常にError |
| 未対応版、出自不明、復元不能ロック、必須モジュール不足 | 失敗ポリシーに従う |
| 独立した任意項目だけが非対応 | 項目をSkipし、MaterialごとにまとめてWarning |
| 機能有効化に必要な項目が不足 | 不完全な有効化を避け、Material全体を除外 |
| 生成・ロック・出力検証の実行中に失敗 | 常にError。成功扱いにしない |

成功、部分適用、除外の件数を分け、警告にはアダプターID、対象Material、導入版、理由を付ける。生のProperty不足だけではなく、利用者が修正できる内容を示す。

## 10. 依存関係・ファイル構成案

追加Shaderは任意依存とする。Poiyomi未導入、ShaderCore未導入でも現行lilToon機能をコンパイル・実行できること。

最初の共通化は既存パッケージ内で行う。Unity APIだけで可能なProperty操作はEditor内アダプターで実装し、外部Editor APIが必要な連携は別assemblyに分離する。必要に応じてVPMアドオン化し、共通Runtime設定型からは依存先Editor型を排除する。

```text
Editor/
  Adapters/
    IDistanceFadeAdapter.cs
    AdapterRegistry.cs
    LilToonDistanceFadeAdapter.cs
    PoiyomiDistanceFadeAdapter.cs
    NonToonDistanceFadeAdapter.cs
  Planning/
    MaterialChangePlan.cs
    AdapterCapabilities.cs
    ShaderGenerationCache.cs
  Integrations/
    Poiyomi/                 # 型依存が必要なら専用assembly／アドオン
    ShaderCore/              # 対応版別のAPI接続
Tests/Editor/
  Compatibility/
  AdapterTests/
  BuildIntegrationTests/
```

コンパイル条件を付けただけで、存在しないassembly参照が解決されると仮定しない。未導入構成でもassembly定義が解決できることを検証する。Shaderや依存ツールを無断で同梱せず、利用者が各配布元から導入する。

## 11. 受入テスト

既存31件を維持したうえで、以下を追加する。現時点では実施計画であり、Poiyomi／NonToon対応済みの根拠ではない。

| ID | 試験 | 期待結果 |
| --- | --- | --- |
| MS01 | 旧Scene／Prefabを新実装で開く | lilToon値・適用状態・GUID参照を保持、新規ShaderはOFF |
| MS02 | 旧設定でInspector表示・ビルド | 元データを無断移行・保存しない |
| MS03 | 3系統のMaterialを混在 | 各設定は対応Shaderにだけ反映、相互汚染なし |
| MS04 | 未導入の追加Shaderを含む構成 | コンパイル成功、lilToonの既存動作を維持 |
| MS05 | Shader別処理OFF／全項目OFF | 複製・生成・ロックを起こさない |
| MS06 | 同名Propertyを持つlilToonとNonToon | lilToonのzとNonToonの独立強度を混同しない |
| MS07 | 1個のMaterialを複数Renderer／Slotで共有 | 1個の出力Materialを共有、nullとSlot順を保持 |
| MS08 | 曖昧なShader名・複数アダプター一致 | 誤適用なし。複数一致はError |
| MS09 | PoiyomiのMode 0／1、近距離／遠距離色 | Object／PixelとRGB乗算の意味を正しく反映 |
| MS10 | PoiyomiのTheme、Alpha、Cullの組合せ | 保証範囲と診断が一致、描画設定を無断変更しない |
| MS11 | Poiyomi未ロック→自動ロック | 最終描画にも設定が残り、機能が削除されない |
| MS12 | Poiyomiロック済み・機能削除済み | 対応経路なら複製だけ再生成。未対応なら除外／Error |
| MS13 | PoiyomiのA／RA設定・名前依存アニメーション | 既存バインディングとリネーム情報を保持 |
| MS14 | NonToonモジュール組み込み済み | 最終Property名を解決し、x／yと強度だけ変更 |
| MS15 | NonToonモジュール未組み込み | 既定では除外。自動追加ONの対応版では複製Shaderへ追加 |
| MS16 | 同名宣言を持つ他モジュールを併用 | モジュールIDを基に解決し、別モジュールを変更しない |
| MS17 | モジュール追加・生成失敗 | 元`.scshader`、Importer、選択構成とRenderer参照を保持 |
| MS18 | 距離片側だけ上書き、同値、逆順、NaN | 最終ペアを検証し、無言の並べ替え・不定計算を防ぐ |
| MS19 | MA／TTTの後段置換とAAOを併用 | 最終Materialへ適用され、最適化後に設定が残る |
| MS20 | ShaderCore外部生成ツールを併用 | 生成元を追跡できる場合だけ適用、順序とモジュールを保持 |
| MS21 | 連続ビルド・別アバター・共有元Shader | Material／生成Shaderキャッシュが混入しない |
| MS22 | Shader別集計と混在Renderer | 全体Renderer数を二重計上せず、集計のみでは変更なし |
| MS23 | Play Mode、VRChatビルド、VR・鏡・カメラ | 距離基準と色の意味を実際の描画で確認 |
| MS24 | WarnAndSkip／FailBuildを切替 | 除外・失敗が設定どおりになり、成功件数を偽らない |

非破壊試験では元Material、元Shader、`.scshader`、`.scmodule`、Importer設定のハッシュ・Dirty状態・参照を比較する。値検証に加え、ロック／生成後の描画を確認する。最小構成の自動試験と実アバターでの統合試験を分けて記録する。

## 12. リリース判定と未確定事項

次を満たしたアダプターから段階的に対応済みとして公開する。

1. バージョン別のShader識別、Property型・最終名、距離基準が確定している。
2. 未導入構成と旧データ互換性、既存lilToonテストが通る。
3. ロック・モジュール生成・定数化の対応範囲が実装とREADMEで一致している。
4. TTT後・AAO前と追加Shaderの最適化順が、ビルドログと最終描画で検証されている。
5. 元Asset不変、共有保持、失敗時の除外／停止を検証している。

実装開始時に優先して確定する項目は、Poiyomiの非破壊ロックAPIとSDKコールバック順、ShaderCore 0.1.11の最終Property名解決とビルド専用Shader生成API、NonToonのheadDepthの生成・単位・VR動作である。調査済みの名称だけで対応範囲を広げない。

Poiyomiの10.x、NonToon／ShaderCoreの別版、Android等の別環境は個別に確認する。公開Web資料と可変ブランチは2026-09-08に確認したが、リリース時は検証した版・ソース識別子を改めて固定する。
