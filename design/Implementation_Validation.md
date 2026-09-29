# 実装・検証記録

対象: `com.camellian.lazyfade.liltoon` 0.1.0  
実装日: 2026-09-08

## 実装範囲

仕様書に基づき、設定コンポーネント、専用Inspector、NDMFの早期検証・適用パス、非破壊Material複製、共有保持、診断、出力コンポーネント除去、Edit Modeテストを実装した。

パッケージIDは仮の`com.example...`から`com.camellian.lazyfade.liltoon`へ変更した。配布用コード・メタデータは`Packages/com.camellian.lazyfade.liltoon`に格納している。VPMリポジトリへの公開は実施していない。

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
- 仕様書のモジュール案にある`ValidateSettingsPass`は、`LazyFadeLilToonPlugin`の早期検証コールバックと`SettingsValidator`へまとめた。
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

## 除外マテリアルのリスト表示一本化（2026-09-09）

除外マテリアルの外側のFoldoutと`showExclusions`を削除し、標準の配列PropertyField自体を「除外マテリアル」として直接表示する構成に変更した。見出しを1回開くだけで要素を編集でき、配列を開いている場合だけ説明を表示する。データ構造・除外規則・集計は変更していない。

既存のInspector描画テストから外側Foldoutの操作を除去し、配列自体を展開した状態で検証した。`./scripts/Run-Tests.ps1`でコンパイル成功、**54件合格／失敗0件／スキップ0件**。READMEとPages原稿も1段のリスト操作に合わせて更新した。

## 組み込みプリセット（2026-09-09）

`Editor/LazyFadeLilToonPresets.cs`の`BuiltIn`配列に、名前と6項目を変更できる仮プリセット3件を追加した。値は利用者が指定するため、3件とも現在の初期値を仮設定した。Runtimeコンポーネントの保存フィールドやGUIDは変更していない。

「2. 基本設定」の先頭のプルダウンで選択すると、距離フェード4項目・リム2項目の保存値を即時上書きする。読み込みボタンは設けない。チェックOFFの項目も保存値は更新するが、チェック状態・除外・高度な設定・コンポーネントの有効状態は維持する。NDMFは既存の処理で更新済みの値を取り込むため、元Materialを編集時に書き換えない。

選択名はInspectorの表示状態だけとし、新しく開くと「現在の設定（個別指定）」から始まる。基本項目の手動編集や初期色ボタンで個別指定表示へ戻る。プリセットを継続的に再適用する処理、選択IDのシリアライズ、専用Undo／Redo操作は追加していない。

空の名前、非有限値、範囲外のリム指数は選択時にエラー表示し、書き込み前に拒否する。HDR ColorとAlphaは保持する。

追加テストは、各3プリセットから実際のNDMF出力への6値の反映、既存Materialの不変性、値のみ変更してチェックとオプションを維持する動作、不正な名前・色・リム指数による部分上書きの防止の7件。既存Inspector描画テストにもプリセット欄を含む。UnityのコンパイルとEdit Modeテスト **61件合格／失敗0件／スキップ0件**。結果XMLは`.verification~/editmode-results.xml`、ログは`.verification~/editmode.log`。プルダウンの手動クリックによる目視確認は未実施。

## リリース前の再確認（2026-09-09）

現在のプリセット定義は`warm`・`cold`・`none`の3件となっている。名前・6項目の値とRuntimeの保存データを変更せず、色の有限値検証の重複、未使用using、モード表示の中間配列変換、重複したテスト属性を整理した。READMEとPages原稿に現在のプリセット値を掲載し、集計ボタン名を「再集計」に同期した。

変更後にUnity Edit Modeを再実行し、**61件合格／失敗0件／スキップ0件**を確認した。検証スクリプトの安全性テストはPowerShell 7.6.5とWindows PowerShell 5.1で各19件合格。公開候補73ファイルとDOCX内18 XMLの機密情報パターン検査、Git除外規則、JSON、メタデータ、文書リンクも再確認した。詳しい確認範囲と残る制限は[公開前レビュー](Public_Release_Review.md)に記載した。

## Unitypackage作成・検証（2026-09-10）

`scripts/Build-UnityPackage.ps1`で、0.1.0のUnitypackageを作成した。配布先は`Assets/BulkDistanceFade`とし、Runtime・Editor・LICENSE・CHANGELOG・Unitypackage用READMEと.metaだけを同梱する。SDK・NDMF・lilToon・Tests・VPM／UPMのmanifestは含めない。Packages版と同じScript／assemblyのGUIDを維持するため、両形式の二重導入は不可と案内した。

Unity 2022.3.22f1の独立した一時プロジェクトでコンパイル・出力後、再インポートとコンパイル検証を別のUnity起動で実行した。最初の試行では非同期インポートの完了前に検証したため失敗し、作成スクリプトを修正した。最終実行は正常終了し、取り込み前後の**37ファイル（.metaを含む）のSHA256がすべて一致**した。

最終成果物は`artifacts/0.1.0/BulkDistanceFade-0.1.0.unitypackage`、16,689バイト。SHA256は`ef08103af8e7f86c4c89a525c306a35505e0f1640b8cd681c7810dc975aa2c69`。アーカイブ内部の20アセット（C# 12ファイルを含む）について、許可した導入パスだけを含むこと、元GUIDとソース本文の一致を別途検証した。公開候補77ファイルとDOCX内18 XMLの秘密情報パターン検査は該当0件。パッケージ本体の処理は変更しておらず、Edit Mode 61件の直前の合格結果を維持する。

日本語のリリース本文は[release/0.1.0.md](../release/0.1.0.md)、再作成手順は[release/README.md](../release/README.md)にある。成果物と同じフォルダーにリリース本文と`SHA256SUMS.txt`も用意した。Gitタグ作成とGitHubへの公開は行っていない。

## NonToon用の別コンポーネント（2026-09-10）

`com.camellian.lazyfade.nontoon`パッケージと`LazyFadeNonToon`を追加した。既存lilToon用のRuntime／EditorとGUIDは変更せず、同一アバタールートに各型1個を併設できる構成とした。元の統合コンポーネント案はユーザー指定により変更した。

ShaderCore 0.1.11／NonToon 0.1.3の公式ソースを取得し、検証環境だけへコピーした。モジュールの公開パーサーを使ってProperty名を解決し、公式ShaderのGUID、版、実Property型、モジュール属性、定数化なしを確認する。距離Vectorのx/yだけを部分上書きし、独立した強度を設定する。元Shaderのモジュール追加や再生成は行わない。

新しいInspector、名前と3値を編集するプリセット3件、独立した除外、起源追跡、読み取り専用集計、NDMF適用を実装した。初期チェックは全OFF。最終距離ペアは保持値も含めて検証する。

`./scripts/Run-Tests.ps1 -IncludeNonToon`の最終実行は**88件合格／失敗0／スキップ0**（lilToon 61件、NonToon 27件）。Unity 2022.3.22f1、SDK 3.10.3、NDMF 1.13.1。2026-09-10 10:13:54～10:13:57 UTC。公式NonToon／NonToonFurの生成Shader、全8通りの部分上書き、共有・非アクティブ・除外・置換起源、入力不正、保存失敗、プリセット、Inspector展開、実NDMFでの両コンポーネント併設を検証した。初回のPackageInfo型名の競合は明示aliasで修正した。

テストスクリプトへ追加assemblyの選択を追加し、Editor本体の終了を待つ方式にして子サービスの終了待ちを避けた。検証依存を準備する`Prepare-NonToonVerification.ps1`は既存コピーを上書きしない。従来のlilToon用Unitypackageは更新しておらず、NonToon用コードを含まない。両パッケージの45 GUIDに重複なし、公開候補114ファイルの秘密情報パターン該当0件、ローカルMarkdownリンク179件を確認した。検証依存の再準備は既存コピーを拒否し、manifest不変であることも確認した。

## 残る手動検証・リリース条件

- MAでの衣装追加、TTTでのMaterial差し替え、AAO最適化を含む実アバターでの最終出力確認。
- MA／TTT／AAOの全8構成の互換性試験と各採用版の実行ログ確認。
- Play Mode移行と実際のVRChatアップロード、Shader機能削減設定、同距離等の境界値の見た目。
- Inspectorの目視確認、Prefab override、Undo／Redo、アニメーションで上書きされる場合の挙動。
- Android等、Windows Editor以外の対象環境。

これらは自動Edit Modeテストの合格だけでは確認済みとしない。依存バージョン範囲の宣言は、範囲内の全組合せの検証完了を意味しない。

## 2026-09-29：lazyFade 0.2.0への改名と配布準備

公開表示、パッケージID、コンポーネント型・ファイル名、主要設計書名をlazyFadeへ統一した。lilToonはcom.camellian.lazyfade.liltoon／LazyFadeLilToon、NonToonはcom.camellian.lazyfade.nontoon／LazyFadeNonToon。既存のnamespace／assembly名と45個の.meta本文・GUID、保存フィールドを保持した。旧名はMovedFrom、旧Prefab互換性テスト、移行表、過去成果物の記録に限って残す。

改名前のソースはローカルの.verification~/before-lazyFade、旧0.1.0成果物は.verification~/legacy-artifactsに保管した。過去の成果物に新しい名前を付け直して配布することはしていない。ルートのWordモックはlazyFade_lilToon_Spec_Mock.docxへ改名し、公開対象から除外した。

### 実施結果

- Unity 2022.3.22f1／SDK Base・Avatars 3.10.3／NDMF 1.13.1／lilToon 2.3.2／ShaderCore 0.1.11／NonToon 0.1.3。
- Edit Mode **90件合格、失敗0、スキップ0**。lilToon 62件、NonToon 28件。旧GUIDと旧m_EditorClassIdentifierを持つPrefabの読み込み・値保持を各1件追加。
- lilToon版：**16,096 bytes**。37ファイル（metaを含む）のUnity出力・再インポート・SHA256一致、再コンパイルを確認。
- NonToon版：**11,025 bytes**。27ファイル（metaを含む）の同じ検証に合格。
- Test-VerificationSafety.ps1：19チェック合格。
- Test-ReleasePipeline.ps1：ZIPルート配置、manifest・依存、ZIPと一覧のSHA256、同一入力再生成、過去版保持、同版改変拒否、公開URL条件、秘密パターン検出、パストラバーサルZIP拒否を隔離コピーで検証。
- Test-PublicationSafety.ps1：公開ソースと両unitypackage・Release添付文書の226テキストペイロードで検出0。現在版とソースの一致、配布パス、SHA256SUMSも検証。
- 全PowerShellスクリプトの構文エラー0、Markdownのローカルリンク切れ0。

| ファイル | SHA256 |
| --- | --- |
| lazyFade-lilToon-0.2.0.unitypackage | 947c7d621c78d6d150fc7c30968d2d7a4b5f7770b6f250aa1b83716f55a465e6 |
| lazyFade-NonToon-0.2.0.unitypackage | 2b6a40735867dd1fe08fd551345df6026f991b34117e75692e02ca64a22a9a17 |

### 公開時に残る作業

公開GitHubの所有者／リポジトリと公開用連絡先メールは未設定。Build-VpmRepository.ps1へ指定すると実URL入りの両ZIP、docs/index.json、SHA256SUMSを生成する。公開URLを仮定した成果物は実際のartifactsへ置かず、隔離テスト内だけに生成した。

GitHubへのpush、Releaseの公開、Pages設定、公開JSON・ZIPへのアクセス、VCCでの実インストールは未実施。実アバターのVR／鏡／カメラ／Android・実アップロード・他ツール混在の見た目は以前と同様に手動確認が必要。
