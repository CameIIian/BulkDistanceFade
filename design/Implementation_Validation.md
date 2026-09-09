# 実装・検証記録

対象: `com.camellian.liltoon-distance-fade` 0.1.0  
実装日: 2026-09-08

## 実装範囲

仕様書に基づき、設定コンポーネント、専用Inspector、NDMFの早期検証・適用パス、非破壊Material複製、共有保持、診断、出力コンポーネント除去、Edit Modeテストを実装した。

パッケージIDは仮の`com.example...`から`com.camellian.liltoon-distance-fade`へ変更した。配布用コード・メタデータは`Packages/com.camellian.liltoon-distance-fade`に格納している。VPMリポジトリへの公開は実施していない。

## API照合・検証環境

| 項目 | バージョン |
| --- | --- |
| Unity | 2022.3.22f1 |
| VRChat SDK Base / Avatars | 3.10.3 |
| NDMF | 1.13.1 |
| lilToon | 2.3.2 |
| Unity Test Framework | 1.4.6 |
| 検証プラットフォーム | Windows Editor、Edit Mode |

インストール済みの依存パッケージを読み取り、作業フォルダー内の`.verification~/Unity`にコピーして検証環境を構築した。既存のアバタープロジェクトは変更していない。検証用コピーは配布物に含めない。

## 実装時に確定した事項

- lilToon 2.3.2のShaderディレクトリから、公開Material用の52個のShader名を抽出し`OfficialShaders.cs`へ記録した。`Hidden/ltspass_*`等の内部パス専用Shaderは除外した。
- Mode表示はlilToonの`lilLanguageManager.cs`を照合し、0=頂点、1=オブジェクト位置とした。未知の値を壊さない数値入力を採用した。
- ルート設定は`VRC.SDKBase.IEditorOnly`を実装し、NDMF適用後にも明示的に設定コンポーネントだけを除去する。
- 生成Materialの登録には`BuildContext.AssetSaver.SaveAsset`を使用する。元Assetの保存APIは呼ばない。
- NDMFの`IError`実装で対象オブジェクトへの参照を持たせる。設定不正は`ErrorSeverity.Error`、補正・非対応Propertyは`NonFatal`で報告する。早期検証失敗はビルド単位の状態にも記録し、後段の適用を抑止する。
- 仕様書のモジュール案にある`ValidateSettingsPass`は、`DistanceFadePlugin`の早期検証コールバックと`SettingsValidator`へまとめた。
- TTT 1.0.1の`NDMFPlugin.cs`／`TTTPass.cs`を照合し、Optimizingでも適用処理があることを確認した。最終出力に設定するため、本ツールもOptimizing内のTTT後・AAO前へ移動した。設計書を1.1に改訂した。AAO側の主要処理もOptimizingにあることをソースで確認した。
- フレネル指数のInspector Clampは値を編集したタイミングで行う。Inspectorを表示・集計しただけではシリアライズ済み値を書き換えない。ビルド時は設定スナップショット内で補正する。

NDMF APIの確認資料: [BuildContext](https://ndmf.nadena.dev/api/nadena.dev.ndmf.BuildContext.html)、[IAssetSaver](https://ndmf.nadena.dev/api/nadena.dev.ndmf.IAssetSaver.html)、[ErrorReport](https://ndmf.nadena.dev/api/nadena.dev.ndmf.ErrorReport.html)。実装ではローカルに導入済みの1.13.1のソースも照合した。

## 自動テスト

Unityのバッチ実行でコンパイル成功、Edit Modeテスト **31件合格／失敗0件／スキップ0件** を確認した（2026-09-08）。NDMF経由の適用と、Optimizing内の先行パスがMaterialを置換した後に本ツールが適用される回帰試験を含む。

結果XML: `.verification~/editmode-results.xml`  
Unityログ: `.verification~/editmode.log`

対象は次のとおり。

- 全プロパティ、HDR Alpha、未知Modeの適用と元Materialの不変性。
- 共有Material、複数Slot、null、非lilToon混在、非アクティブ・無効Renderer。
- Vector4の部分上書き、全OFF、コンポーネント無効。
- 子配置、重複、Descriptorなし、非有限値、フレネル指数補正。
- 読み取り専用集計、保存失敗時の参照保持、連続処理時のキャッシュ独立性。
- Strict候補判定、一部／全Property非対応とWarningの重複排除。
- 実際のNDMFパイプラインを通したプラグイン登録・適用・設定除去・編集元アバター保持。
- Optimizing内のテスト用先行プラグインによるMaterial置換後の再走査・適用。
- 実際に保存した元Materialアセットのファイルバイト列とDirty状態の保持。

パッケージのJSON／assembly定義の構文、全ファイルの`.meta`付属、GUIDの重複なしも確認した。Runtime assemblyにはUnityEditor参照を含めない。テスト用Shaderとテスト用NDMFプラグインはTestsフォルダーに置き、テストassemblyは通常ビルドへ含めない。

## マテリアル除外機能の追加検証（2026-09-09）

[マテリアル除外機能 設計書](Material_Exclusion_Spec.md)を先に作成し、`excludedMaterials`、Inspector配列編集、スナップショット内の除外集合、適用前フィルター、除外Material／Slot集計を実装した。NDMF 1.13.1のローカル`ObjectRegistry.cs`／`ObjectReference.cs`を照合し、登録済みの置換起源を読み取り専用で比較する。未登録の置換は直接参照が一致する場合だけ除外する。

Unity 2022.3.22f1のバッチ実行でコンパイル成功、Edit Modeテスト **48件合格／失敗0件／スキップ0件** を確認した。既存31件に対し17ケースを追加した。実行コマンドは`./scripts/Run-Tests.ps1`、依存パッケージと検証プロジェクトは前述の環境を使用した。

- 同名の別Material、共有・混在Slot、非アクティブ／無効Renderer、SkinnedMeshRendererの除外と集計。
- 空／null配列、重複、null／削除済み参照、未使用Material、全除外、全項目OFF、コンポーネント無効。
- 不足Property警告の抑止、不正設定の検証維持、スナップショットの独立性、集計時の設定・Material・Renderer・Registryの不変性。
- NDMF全パイプライン経由での直接除外、登録済み／未登録の先行Material置換、登録された連鎖・分岐、次回ビルドへの状態非持ち越し。
- 除外元Materialアセットのファイルバイト列とDirty状態の保持。
- SerializedObjectによるUndo／Redo、Prefab保存・読み込み・インスタンスoverride、除外フィールドを含まない旧Prefabデータの読み込み。

初回のサンドボックス内実行ではUnityライセンスを取得できず、許可された通常ユーザー環境で再実行した。初回のテスト実行は47件合格・1件失敗で、保存済みMaterialのC#ラッパーを`SameAs`で比較するアサーションが原因だった。UnityのInstance IDによる同一性確認へ修正し、全48件の再実行が合格した。

最新結果XML: `.verification~/editmode-results.xml`  
最新Unityログ: `.verification~/editmode.log`

Inspectorの目視・ドラッグ操作、実MA／TTT／AAO構成、実アップロードは今回も未実施。置換追跡テストには、NDMFへ置換情報を登録する専用の先行テストプラグインを使用した。

## 裏面・モードのUIと初期適用状態の変更（2026-09-09）

`overrideBackfaceShadow`と`overrideMode`の新規追加時の初期値をfalseへ変更した。裏面の値はboolのチェックボックス、モードは「頂点」(0)／「座標」(1)のプルダウンとした。未知Mode値は別の表示項目として保持し、Inspectorの表示だけで変更しない。既存Scene／Prefabに保存された適用状態は維持する。

手動編集で`backfaceShadow`へ代入されていたColor値はbool型と不整合のためfalseへ修正した。同時に存在していた色・開始距離・強度の初期値やInspectorラベルの手動変更は保持した。

既存の全項目適用テストは裏面・モードを明示ONにするよう変更し、部分上書きテストは試験用の開始距離を明示した。新規コンポーネントがMaterialの裏面・Modeを保持する回帰テストを1件追加した。`./scripts/Run-Tests.ps1`によるUnity 2022.3.22f1のコンパイルとEdit Modeテストは **49件合格／失敗0件／スキップ0件**。結果は`.verification~/editmode-results.xml`、ログは`.verification~/editmode.log`。Inspectorの目視操作は未実施。

## 初期色のRGB変換と保存済み設定の修正操作（2026-09-09）

利用者の意図するフェード色は0～255表記のRGB `(10, 7, 7)`、Alpha 1（`#0A0707`）であることを確認した。`Color(10, 7, 7, 1)`は0～1へ正規化されず、RGBが1を超えるHDR値となっていた。リム色の`Color(255, 188, 177, 0)`にも同じ問題があった。

初期色を`Color32`から`Color`への変換で定義した。フェード色は`Color32(10, 7, 7, 255)`、リム色は`Color32(255, 188, 177, 0)`とし、Alpha 1／0を維持する。Color32はAlphaを含め各成分が0～255である。[Unity Color32仕様](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Color32.html)

既存Scene／Prefabの保存値は自動変換せず、各色の右に「初期色」ボタンを追加した。その色の適用ON時に使用でき、選択した色だけを初期化する。SerializedProperty経由でUndo／RedoとPrefab overrideの仕組みを利用する。既存のHDR入力・非破壊Material適用・ユーザー調整済みの距離や強度、リムの指数4.5は保持する。

HDRピッカーのIntensityは色の値から算出される表示で、本ツールの強度とは別である。色のウィンドウを開き直した際にRGB表示とIntensityの内訳が再計算され得るため、Intensity表示そのものを独立した保存値とは扱わない。[Unity HDRカラーピッカー](https://docs.unity3d.com/2022.3/Documentation/Manual/HDRColorPicker.html)

`./scripts/Run-Tests.ps1`でコンパイルとEdit Modeテスト **52件合格／失敗0件／スキップ0件**。追加3ケースでは、初期色の正規化・Alpha・複製Materialへの適用と元Materialの不変性、フェード色／リム色それぞれの初期化・他項目保持・Undo／Redoを確認した。既存の任意HDR値を適用するテストも合格した。結果XMLは`.verification~/editmode-results.xml`、ログは`.verification~/editmode.log`。カラーピッカー表示・実アバターの描画は未目視確認。

## Inspectorの操作順・折りたたみ・項目チェックの整理（2026-09-09）

画面を「1. 処理の有効化」「2. 距離フェードの基本設定」「オプション」「3. 集計・確認」の順へ変更した。基本項目は開始距離・終了距離・色・強度とし、リム設定、除外マテリアル、高度な設定（裏面・モード・Strict）を、それぞれ初期状態で閉じたFoldoutに格納した。エラー・警告・集計・更新ボタンはFoldoutの外に表示する。

開閉状態はInspectorインスタンスのboolだけに保持し、アバター設定をシリアライズしない。開閉で値や適用状態を変更しない。リムの見出しには適用項目数、除外の見出しには空欄・重複を含む配列の登録枠数を表示する。

各項目の適用チェックは、文字ラベルを削除して幅18のチェックボックスへ変更した。項目名とOFF時の既存値保持はツールチップで説明する。SerializedPropertyによる編集・Undo／Redo・初期色ボタンの経路は維持した。

最終状態で`./scripts/Run-Tests.ps1`を実行し、Unityのコンパイル成功、Edit Modeテスト **52件合格／失敗0件／スキップ0件** を確認した。Foldoutの開始・終了の対応、旧チェックラベルの削除、README・Pages原稿の説明とMarkdown変換も確認した。Unity Inspectorの目視操作・幅別レイアウト確認は未実施。

## リムの常時表示と除外配列GUIの修正（2026-09-09）

「2. 基本設定」内に距離フェード4項目とリム2項目を常時表示し、リムのFoldoutを除去した。オプションは除外マテリアルと高度な設定だけとした。

除外Material配列の標準PropertyFieldが内部でFoldoutHeaderGroupを使用するため、外側のFoldoutHeaderGroupと入れ子になって描画エラーが発生していた。外側の折りたたみは通常の`EditorGUILayout.Foldout`へ変更し、ヘッダーグループを作らない構造にした。

集計は確認用の読み取り専用走査であり、ボタン未実行でもNDMF適用は最新設定を取り込んで再走査することを、InspectorとREADME／Pages原稿へ明記した。

UnityのEditorWindowに実際のカスタムInspectorを描画する回帰テストを追加した。除外と高度な設定を開き、2要素の除外配列も展開した状態でRepaintが発生し、予期しないログがなく、設定が書き換わらないことを確認する。別のテストでは、集計後に除外設定を変更し、再集計なしでも最新の除外設定で適用されることを確認した。

最初の実行は既存の`-nographics`指定により描画デバイスがなく、描画テストだけ失敗した。このテストに必要なグラフィックスを有効にするため、`scripts/Run-Tests.ps1`から`-nographics`を除去し、`-batchmode`は維持した。再実行でコンパイル成功、**54件合格／失敗0件／スキップ0件**。結果XMLは`.verification~/editmode-results.xml`、ログは`.verification~/editmode.log`。画面描画の実行とエラーの不在を確認したもので、幅ごとの見た目や全操作の目視確認とは区別する。

## 残る手動検証・リリース条件

- MAでの衣装追加、TTTでのMaterial差し替え、AAO最適化を含む実アバターでの最終出力確認。
- MA／TTT／AAOの全8構成の互換性試験と各採用版の実行ログ確認。
- Play Mode移行と実際のVRChatアップロード、Shader機能削減設定、同距離等の境界値の見た目。
- Inspectorの目視確認、Prefab override、Undo／Redo、アニメーションで上書きされる場合の挙動。
- Android等、Windows Editor以外の対象環境。

これらは自動Edit Modeテストの合格だけでは確認済みとしない。依存バージョン範囲の宣言は、範囲内の全組合せの検証完了を意味しない。
