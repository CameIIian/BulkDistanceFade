# マテリアル除外機能 設計書

文書バージョン: 1.0  
作成日: 2026-09-09  
対象: 現行lilToon Distance Fade Bulk Setterへの追加機能

## 1. 目的・適用範囲

利用者が指定したMaterialを距離フェードの一括適用から除外する。アバタールートの既存コンポーネント1個で管理し、除外Materialを参照するすべての対象Renderer／Slotへ適用する。非アクティブGameObject、無効Renderer、MeshRenderer／SkinnedMeshRendererも同じ規則で扱う。

本書は既存仕様の「Material除外リストは対象外」を更新する追加仕様である。マルチShader拡張仕様の手動Material除外も本書へ切り出す。今回の実装対象は現行lilToon処理であり、Shaderアダプター化は前提としない。

## 2. 設定データ・Inspector

- `DistanceFadeBulkSetter`に公開シリアライズフィールド`Material[] excludedMaterials`を追加する。初期値は空配列。既存フィールド、クラス名、assembly、スクリプトGUIDを保持する。
- 空配列、配列自体のnull、未設定／削除済み参照は除外指定なしとして扱う。重複指定は1件として判定する。未使用Materialの指定はエラーにしない。
- InspectorのStrict設定の下に「除外マテリアル」の配列編集UIを表示する。Unity標準の`SerializedProperty`を使用し、追加・削除・参照変更、Undo／Redo、Prefab overrideをサポートする。
- 全スロットへ適用すること、未設定要素を無視することを説明する。編集時集計に除外Material数と除外Slot数を追加する。設定編集とUndo／Redoで既存の集計更新案内を表示する。
- `SettingsValidator.Capture`は有効参照を独立した集合へコピーする。元配列の整理や書き換えを行わず、取得後の配列変更にも影響されない。

## 3. 一致規則・他ツールとの連携

1. 適用時にRendererが参照するMaterialと、除外指定のUnityオブジェクト参照が一致すれば除外する。名前、Shader、Property値による一致判定は行わない。
2. NDMFビルド中は、現在のObjectRegistryに登録済みの`ObjectReference`が一致するMaterialも除外する。先行ツールが`RegisterReplacedObject`で登録した複製・連鎖置換・同一起源の複数出力に対応する。
3. Registry参照は`IObjectRegistry.GetReference(material, false)`で読み取る。集計自体が新規登録しないようにし、編集時にRegistryがなければ直接参照だけで判定する。
4. 置換情報が未登録の別Materialは元Materialから追跡できないため、自動除外しない。MA／TTT全構成への保証ではなく、登録情報がある場合の追跡とする。複数Materialの統合もRegistryで表現された起源の範囲だけ対応する。

除外は本ツールによる値変更・複製・保存・Slot置換の抑止を意味する。他ツールの変更や最適化を抑止するものではない。

## 4. 処理手順・集計

既存の配置・有限値検証、OptimizingのTTT後／AAO前という順序を維持する。すべてのMaterialを除外しても、不正な有効設定は既存どおりエラーになる。

`Collect`のSlot判定順は、全項目OFF → null Material → 手動除外 → null Shader → Shader候補／対応Propertyとする。除外MaterialではShader判定・不足Property警告を実施しない。全項目OFFでは従来のSkip理由を優先し、除外数は0とする。コンポーネント無効時も従来どおり処理せず、出力から設定だけ除去する。

- 除外Materialは適用計画と複製キャッシュに追加しない。混在Rendererでは対象Slotだけ置換し、除外Slotの参照・並び・値を維持する。
- 除外のみのRendererは対象Renderer数に含めない。全除外でも設定コンポーネントを出力から除去する。
- `BuildSummary.ExcludedMaterials`は走査で遭遇した除外Materialのユニーク数とする。登録配列の要素数や置換前起源数ではない。
- `BuildSummary.ExcludedSlots`と`SkippedSlots["手動除外"]`は除外Slot数とする。Inspectorとビルドログの両方に表示し、通常の除外を警告扱いにしない。
- 除外集合・判定キャッシュは取得／走査単位とし、別アバター・次回ビルドに共有しない。

## 5. テスト・受入条件

既存Edit Modeテスト31件に加え、以下をUnity 2022.3.22f1、NDMF 1.13.1、lilToon 2.3.2で検証する。

| ケース | 期待結果 |
| --- | --- |
| 初期値／空／null配列、未設定・削除済み・未使用・重複要素 | 既存動作を保持、例外なし、二重集計なし |
| 同名の別Material、混在Slot、共有、非アクティブ・無効Renderer | 指定参照だけ除外、対象のみ複製し共有を保持 |
| 全除外、コンポーネント無効、全項目OFF | 複製・保存なし、設定除去、規定の集計 |
| 除外した部分対応Shader | 不足Property警告なし |
| スナップショット・読み取り専用集計 | 元設定／Material／Renderer／Registryを変更しない |
| 元Materialアセット | ファイルバイト列とDirty状態を保持 |
| NDMFパイプライン | 編集元設定保持、出力設定除去、除外を適用 |
| 登録済みの連鎖置換・分岐、未登録置換、次回処理 | 登録した同一起源だけ除外、ビルド間で持ち越さない |
| シリアライズ・Undo／Redo・Prefab | 除外参照の保存と復元、旧データは除外なし |

実行結果は`Implementation_Validation.md`へ追記する。Inspectorの目視操作、実MA／TTT／AAO混在アバターとVRChatアップロードの確認は、自動テストとは区別して記録する。
