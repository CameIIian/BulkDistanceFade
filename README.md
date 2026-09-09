# BulkDistanceFade

## 概要

[GitHub Pages用のガイド原稿](docs/index.md) · [設計・検証資料](design/README.md) · [ドキュメントの公開手順](docs/publishing.md)

BulkDistanceFadeは、VRChatアバターで使用するlilToonマテリアルの距離フェード設定を、アバタールートの1つのコンポーネントから一括適用するUnity Editor拡張です。NDMFのビルド処理でマテリアルを複製し、その複製へ設定を書き込みます。

lilToon公式の機能ではなく、lilToonに対応する独立した拡張です。現在の実装対象はlilToonであり、Poiyomi・ShaderCore／NonToonには対応していません。

パッケージIDは `com.camellian.liltoon-distance-fade`、`package.json`のバージョンは `0.1.0` です。コードのライセンスは[MIT License](Packages/com.camellian.liltoon-distance-fade/LICENSE)です。依存パッケージにはそれぞれのライセンスが適用されます。

## 特徴

- **項目ごとの上書き指定**：色、距離、強度、裏面、モード、リムの各項目に左端のチェックチェックがあります。OFFの項目はマテリアルの既存値を保持します。
- **元アセットを保持**：本ツールは元Material・Prefab・Textureへ変更を保存しません。生成するMaterialはビルド用です。
- **アバター全体を処理**：ルートと全子孫のMeshRenderer／SkinnedMeshRendererを走査します。非アクティブGameObjectと無効Rendererも対象です。
- **マテリアルの共有を保持**：同じ対象Materialを使うスロットには、1個の複製を共有して割り当てます。
- **マテリアル単位の除外**：指定したMaterialを使うすべての対象スロットを除外できます。
- **編集時の集計と入力検証**：対象件数・除外件数、配置エラー、非対応項目をInspectorで確認できます。

## 導入

1. Unity 2022.3のVRChat Avatarプロジェクトを用意し、VRChat SDK Avatars・NDMF・lilToonを先に導入します。確認済みのバージョンは「対応環境」を参照してください。
2. 次のいずれかの方法で本パッケージを導入します。
   - リポジトリの `Packages/com.camellian.liltoon-distance-fade` フォルダーを、導入先プロジェクトの同じ `Packages/com.camellian.liltoon-distance-fade` の位置へコピーします。
   - UnityのPackage Managerで「Add package from disk」を選び、本パッケージの `package.json` を指定します。
3. Unityのコンパイルが完了し、Consoleにコンパイルエラーがないことを確認します。
4. Add Componentの `BulkDistanceFade → Distance Fade Bulk Setter` からコンポーネントを選択します。

VPM依存情報は `package.json` に含まれますが、公開VPMリポジトリへの登録は行っていません。上記の手動導入では、VPM依存パッケージが自動導入されることを前提にせず、先に依存環境を用意してください。

## 使い方

1. Hierarchyで、**VRC Avatar Descriptorと同じアバタールートGameObject**を選択します。
2. `BulkDistanceFade → Distance Fade Bulk Setter` を1個追加します。
3. 「処理を有効化」がONであることを確認します。
4. 各項目の左端のチェックをONにして、値を入力します。既存値を残したい項目は左端のチェックをOFFにします。
5. 必要に応じて「除外マテリアル」を開き、中の「除外リスト」に処理したくないMaterialを登録します。
6. 必要に応じて「集計を更新」を押し、対象件数・除外件数とエラー表示を確認します。この操作はビルド時の適用に必須ではありません。
7. NDMFが実行されるアバタービルド、またはNDMFによるPlay Mode処理で結果を確認します。実アップロード・Play Modeの手動検証状況は「対応環境」を参照してください。

**Inspectorの編集や「集計を更新」だけでは、Scene内のMaterialの見た目は変わりません。** 本ツールの適用タイミングはNDMFビルド処理です。手動で元Materialへ設定を焼き込むボタンはありません。

設定コンポーネントは1アバターにつき1個です。子オブジェクトに取り付けたり、別の子へ追加して設定を分けたりすることはできません。処理後のビルド出力からは設定コンポーネントを除去します。

## 設定項目

以下は現在のコードでコンポーネントを新規追加したときの初期値です。既にScene／Prefabへ保存された値や適用状態は、その保存内容が使われます。

### 画面の操作順と折りたたみ

基本操作は、上から「1. 処理の有効化」→「2. 基本設定」→「3. 集計・確認」の順に進めます。基本設定には「距離フェード」の開始距離・終了距離・色・強度と、「リム」の色・リムライトの細さの計6項目を常に表示します。最後の集計は任意の確認操作です。

基本設定と集計の間に、初期状態で閉じたオプションの見出しがあります。見出しをクリックすると開閉します。

| 見出し | 中にある設定 |
| --- | --- |
| 除外マテリアル | 除外リストと説明。見出しの「登録枠」は配列の要素数で、空欄や重複も含みます。実際の除外数は集計で確認します。 |
| 高度な設定 | 裏面を陰にする、モード、Strict lilToon Check |

**折りたたみは表示の切り替えです。閉じても設定を無効にしません。** 開閉状態はアバターの保存データには書き込みません。入力エラー、警告、集計、更新ボタンは折りたたみの外に表示します。

各項目の左端は文字ラベルのないチェックボックスです。マウスを重ねると、項目の適用とOFF時の既存値保持について説明が表示されます。

### 全体設定

| 画面の項目 | 初期値・表示 | 操作と意味 |
| --- | --- | --- |
| 処理を有効化 | ON | 本ツールによるMaterial処理を切り替えます。コンポーネント見出しの有効チェックと同じ状態です。OFFでも設定値は保持されます。 |
| 配置先 | コンポーネントを付けたGameObject名 | 表示専用です。配置を変更する入力欄ではありません。VRC Avatar Descriptorと同じGameObjectであることを確認してください。 |
| 対象範囲 | アバター全体（非アクティブを含む） | 表示専用です。階層やRendererを選んで範囲を狭める操作はありません。Material単位の除外を使用します。 |
| Strict lilToon Check | ON | Shader名による候補判定を切り替えます。詳細は次項を参照してください。 |
| 除外マテリアル | 空の配列 | 除外するMaterial参照を登録します。詳細は「除外マテリアル」を参照してください。 |

「処理を有効化」がOFFでも、配置と重複の検証は行います。無効なコンポーネントを子へ残してエラーを回避することはできません。

### Strict lilToon Check

- **ON**：lilToon 2.3.2から抽出した52個の公開用Shader名との完全一致で候補を選びます。
- **OFF**：上記に加え、実装の名前規則に一致するカスタムShaderも候補にします。例えば `Custom/lilToonVariant` は候補ですが、`NotlilToon` は候補にしません。

名前一覧は[OfficialShaders.cs](Packages/com.camellian.liltoon-distance-fade/Editor/OfficialShaders.cs)、カスタム名の判定規則は[MaterialUtility.cs](Packages/com.camellian.liltoon-distance-fade/Editor/MaterialUtility.cs)にあります。OFFは全Shaderへの強制適用ではありません。どちらの設定でも、適用するPropertyがMaterialに存在するかを確認します。名前一致やPropertyの存在だけで、すべての派生Shaderの描画を保証するものではありません。

### 除外マテリアル

1. 「除外マテリアル」を開き、中の「除外リスト」を展開して配列の要素数を増やします。
2. 各要素の参照欄へProject内のMaterialをドラッグするか、オブジェクト選択ボタンから指定します。
3. 除外を解除するときは、その参照をNoneへ戻すか、配列の該当要素を削除します。全解除する場合は要素数を0にします。
4. 編集後に「集計を更新」で除外件数を確認します。

空欄、削除済み参照、重複登録は処理上無視します。アバターで未使用のMaterialを登録してもエラーにはなりません。

判定はMaterialの参照に基づきます。同名の別Materialは別物です。同じMaterialを複数Rendererや複数スロットで使っている場合、それらをまとめて除外します。スロットごとの個別除外には対応しません。除外Materialは本ツールでは複製・設定変更・参照置換をせず、不足Propertyの警告も出しません。

他ツールがNDMFへMaterialの置換元を登録していれば、同じ起源の置換後Materialにも除外を引き継ぎます。未登録の置換や複数Materialの統合では追跡できない場合があります。除外は他ツールの処理を停止するものではありません。

### 各行の有効化チェック

距離フェードとリムの各行には、値の入力欄とは別に左端のチェックチェックがあります。

| 適用状態 | 動作 |
| --- | --- |
| ON | 右側の入力欄を編集できます。ビルド時に、対応するMaterialの値をこの設定で上書きします。 |
| OFF | 右側の入力欄は無効になります。入力済みの値を保持し、Materialには書き込みません。 |

全項目の左端のチェックをOFFにするとMaterialを複製・置換しません。「処理を有効化」がONである限り、適用ONの値は事前に検証するため、すべてのMaterialを除外しても不正な入力値によるエラーは発生します。

### 距離フェード

| 項目 | 入力方法 | 値の初期値 | 適用の初期状態 | 説明 |
| --- | --- | --- | --- | --- |
| 色 | HDRカラーフィールド | RGB `#0A0707`（0～255で `10, 7, 7`）、Alpha `1` | ON | フェード先の色です。RGBとAlphaをまとめて上書きします。 |
| 開始距離 | 数値入力 | `0.18` | ON | 距離フェードの開始側の境界です。 |
| 終了距離 | 数値入力 | `0.01` | ON | 距離フェードの終了側の境界です。 |
| 強度 | 数値入力 | `0.95` | ON | 距離フェードの強度です。`0` を適用するとフェードを無効にする値を上書きします。 |
| 裏面を陰にする | チェックボックス | OFF（false） | **OFF** | lilToonの距離フェードにおける裏面の扱いを切り替えます。 |
| モード | プルダウン | 頂点（0） | **OFF** | 「頂点」（0）または「座標」（1、オブジェクト位置）を選びます。 |

色のフィールドをクリックするとUnityのHDRカラー編集画面を開きます。初期色は0～255表記のRGB `(10, 7, 7)`（`#0A0707`）、Alpha 1です。コードでは `Color32(10, 7, 7, 255)` を `Color` へ変換し、RGBを約 `(0.039216, 0.027451, 0.027451)` として保存します。`new Color(10, 7, 7, 1)` と直接指定すると強いHDR値になるため、0～255表記には `Color32` を使用してください。任意に設定したHDR値を自動でClamp・変換する処理は行いません。Alphaが見た目に与える影響はlilToonの描画モードに依存し、Alphaを下げるだけで不透明Materialを透明表示へ変更する機能ではありません。

開始距離と終了距離は自動で並べ替えません。確認したlilToon 2.3.2の処理では、初期設定のように開始距離が終了距離より大きいと、距離が小さくなる方向へフェードが強くなります。同じ距離の入力も本ツールでは拒否しませんが、同距離などの境界条件の見た目は未検証です。開始・終了距離と強度に、本ツール独自の範囲制限や自動補正はありません。

「裏面を陰にする」には、左端のチェックと右の値のチェックボックスがあります。**適用ON・値OFFは、裏面設定をOFFへ上書きする指定**です。適用OFFは、元Materialの裏面設定を残す指定です。この項目は距離フェードの裏面処理を制御し、ライトの影やカリング設定を変更するものではありません。

モードの「頂点」はメッシュ上の位置に応じた距離、「座標」はオブジェクト原点の位置を基準にする選択です。「座標」用のXYZ入力欄はありません。既存データに0・1以外の値があれば「未対応値 (数値)」を追加表示し、利用者が選び直すまでその数値を保持します。

### リム

ここで設定するリムは、**距離フェードに付随するリム**です。lilToonの独立したRim Light設定一式を編集する画面ではありません。

| 項目 | 入力方法 | 値の初期値 | 適用の初期状態 | 説明 |
| --- | --- | --- | --- | --- |
| 色 | HDRカラーフィールド | RGB `#FFBCB1`（0～255で `255, 188, 177`）、Alpha `0` | ON | 距離フェードのリム色とAlphaをまとめて設定します。確認したlilToon 2.3.2では、Alphaがリム色を混ぜる割合に関わり、初期値のAlpha 0ではリム色の寄与がありません。 |
| リムライトの細さ | 数値入力 | `4.5` | ON | 距離フェードのリムのフレネル指数です。確認した計算式では、値を大きくするとリムの寄与する範囲が狭くなります。 |

「リムライトの細さ」は、入力を編集したときに `0.01～50` へ補正します。保存済みデータが範囲外なら、適用ONのビルド時にビルド用設定だけを補正して警告します。Inspectorの表示や集計だけでは保存済みの値を変更しません。

適用ONの距離・強度・色（Alphaを含む）・リムの指数には有限値が必要です。NaNやInfinityは入力エラーになります。

### 色の「初期色」ボタン

距離フェードとリムの色の右にある「初期色」を押すと、その色だけを現在の初期値へ戻します。押すには、その行の左端のチェックをONにしてください。距離フェードはRGB `#0A0707`・Alpha 1、リムはRGB `#FFBCB1`・Alpha 0になります。他の項目や適用チェックは変更せず、Undo／Redoにも対応します。

既にコンポーネントへ保存された色は、コードの初期値変更だけでは更新されません。旧設定でIntensityが大きい、または色が想定と異なる場合は、該当する色の「初期色」を押してください。HDRカラーピッカー内のIntensityは保存されたRGBから再計算される表示で、本ツールの「強度」とは別です。[UnityのHDRカラーピッカーの説明](https://docs.unity3d.com/2022.3/Documentation/Manual/HDRColorPicker.html)

### 集計・メッセージ・更新ボタン

集計は、編集時点の設定で処理する対象と除外対象を数える読み取り専用の確認機能です。Materialへの設定適用や、適用の予約・確定を行う操作ではありません。

**「集計を更新」を押さなくてもツールは動作します。** NDMFのビルド処理では、Inspectorの集計結果を使わず、最新の設定と構成を検証して対象を再走査します。正しい配置、処理の有効化、適用項目と対応Material、有効な設定値は必要です。

| 表示・操作 | 意味 |
| --- | --- |
| 編集時集計：対象Renderer | 適用できるMaterialを1個以上持つ、対象Rendererの数です。 |
| 編集時集計：対象Material | 実際に適用できるMaterialのユニーク数です。同じMaterialを複数スロットで使っていても1個と数えます。 |
| 手動除外：Material | 走査で遭遇した除外Materialのユニーク数です。除外リストの登録要素数ではありません。 |
| 手動除外：Slot | 除外Materialを参照するスロットの数です。 |
| 集計を更新 | 現在の設定を検証して再走査します。Materialを複製・変更する処理は行いません。 |
| 設定または構成が変更された旨の案内 | 集計が古い可能性を示します。設定変更やUndo／Redoなどの後に更新ボタンを押してください。 |
| 非対応項目の警告 | 適用ONの項目に必要なPropertyがないMaterialの数を示します。詳細はビルド時の警告でも確認できます。 |
| 処理無効・強度0の案内 | 全体処理がOFF、または強度0の適用がONであることを知らせます。 |
| 設定エラー | 配置・重複・入力値の問題を表示します。「トラブルシューティング」に従って修正してください。 |

コンポーネント無効時は集計が0になります。全項目OFFでは除外より「全項目OFF」の判定が優先されるため、除外件数も0です。入力エラーで再集計できない場合は件数が表示されません。再集計前の値が残っている場合は、エラーを直して更新してください。

Inspectorの集計は編集時の構成に対する値です。MA／TTTなどがビルド時にRendererやMaterialを追加・置換すると、ビルド時の件数と異なる場合があります。Consoleにはビルド時の走査・対象・複製・置換・除外の件数と、スキップ理由ごとのスロット数を出力します。

## 対応環境

[package.json](Packages/com.camellian.liltoon-distance-fade/package.json)の宣言と、実際の確認環境を分けて記載します。

| 項目 | 宣言・要件 | API照合／自動テストの確認環境 |
| --- | --- | --- |
| Unity | 2022.3 | 2022.3.22f1 |
| VRChat SDK Avatars | `>=3.10.3 <4.0.0` | Base／Avatars 3.10.3 |
| NDMF | `>=1.13.1 <2.0.0` | 1.13.1 |
| lilToon | `>=2.3.2 <3.0.0` | 2.3.2 |
| Unity Test Framework | 自動テストを実行する場合に必要 | 1.4.6 |
| 検証プラットフォーム | Unity Editor拡張 | Windows Editor、Edit Mode |

Modular Avatar（MA）、TexTransTool（TTT）、Avatar Optimizer（AAO）は任意です。これらへの直接のassembly参照はありません。本ツールはNDMFのOptimizing段階で、MA本体・late transform・TTTの後、AAOの前に実行するよう順序を指定しています。

**依存バージョン範囲内の全組合せについて動作確認したわけではありません。** 実MA／TTT／AAO混在アバター、実際のVRChatアップロード、Play Mode移行時の見た目、Android等の環境、Inspectorの目視操作は手動検証が残っています。先行処理によるMaterial置換は、テスト用NDMFプラグインでも検証しています。

## 注意事項

- 元Materialへの恒久的な書き込み、Prefab Apply、Sceneの即時プレビュー、実行時に設定を操作するUIは提供しません。
- 対象はルート以下のMeshRenderer／SkinnedMeshRendererの `sharedMaterials` です。ParticleSystemRendererなどの別種Renderer、アバター外のRenderer、未使用Materialは処理しません。
- AnimationClip内のMaterialプロパティやMaterial差し替えキーは変更しません。Clipだけが参照するMaterialは走査対象外です。アニメーション再生で、適用した値が上書きされる場合があります。
- MaterialPropertyBlock、Shaderキーワード、描画モードは変更しません。Propertyが存在しても、Shaderの機能削減設定などによって距離フェードが描画されない場合があります。
- 一部のPropertyだけが不足する場合は警告し、対応する項目を適用します。適用できるPropertyが1つもなければ、そのMaterialを複製しません。
- 初期状態で左端のチェックがONの項目は、元Materialの設定を上書きします。現在の初期強度は0.95です。初期リム色のAlphaは0なので、既存の距離フェード用リム設定を残すにはリムの左端のチェックをOFFにしてください。
- コードの初期値を変更しても、Scene／Prefabに保存済みの設定は一括更新されません。既存コンポーネントの値はInspectorで編集してください。
- 除外設定は本ツールだけに適用されます。他ツールによる複製・置換・統合・最適化を禁止するものではありません。

## トラブルシューティング

| 症状・表示 | 確認・対処 |
| --- | --- |
| Add Componentに見つからない | `BulkDistanceFade → Distance Fade Bulk Setter` を探してください。Consoleのコンパイルエラーと、依存パッケージ・本パッケージの導入状態を確認します。 |
| 値の入力欄がグレーになっている | その行の左端のチェックをONにしてください。「裏面を陰にする」と「モード」は初期OFFです。 |
| 設定しても編集画面の見た目が変わらない | 編集時集計はプレビューではありません。NDMFのビルド処理後の出力を確認してください。 |
| 対象件数が0 | 「処理を有効化」、各行の左端のチェック、除外リスト、対象Renderer、Shader名、必要Propertyを確認し、「集計を更新」を押します。 |
| 一部のMaterialだけ反映されない | 除外指定とStrict判定、ビルド時の不足Property警告を確認します。StrictをOFFにしても全Shaderが対象になるわけではありません。 |
| 除外件数が登録数と違う | 件数は走査で遭遇したMaterial／Slotの数です。重複・未使用要素は数えず、全項目OFFやコンポーネント無効時も0です。 |
| 他ツールの処理後に除外が効かない | Materialが別参照へ置換され、NDMFへ起源が登録されていない可能性があります。同名かどうかだけでは追跡しません。 |
| ビルド後もフェードが見えない／再生中に変わる | 強度、距離、色に加え、Shaderの機能削減・描画モードとAnimationClipによる上書きを確認します。 |
| E001：配置エラー | VRC Avatar Descriptorと同じGameObjectへコンポーネントを配置します。子への配置は不可です。 |
| E002：重複エラー | 非アクティブ・無効コンポーネントも含め、アバター全体で設定を1個にします。 |
| E003：有限値エラー | 指摘された適用ONの値を有限値へ修正します。色はAlphaも確認します。適用不要な項目なら左端のチェックをOFFにできます。 |
| E004：適用失敗 | NDMFのエラー報告とConsoleの例外内容を確認してください。メッセージには発生した例外の説明が含まれます。 |
| W001：非対応項目 | 指摘されたPropertyが対象Materialにありません。対応するShader構成を使用するか、その項目の適用をOFF、またはMaterialを除外します。 |
| W002：フレネル指数補正 | 「リムライトの細さ」を0.01～50へ修正します。ビルド時はその範囲へ補正した値が使用されます。 |
| モードに「未対応値」が出る | 保存済みデータに0・1以外の値があります。変更したい場合は左端のチェックをONにして「頂点」か「座標」を選びます。 |

## Build / Test

### アバターのビルド

本パッケージはUnityがC#をコンパイルするEditor拡張です。独立した実行ファイルを作るビルド手順はありません。設定はNDMFのアバタービルド処理で適用されます。VRChat SDKでビルドする前に、Inspectorの設定エラーとConsoleのコンパイルエラーを解消してください。

### 導入先プロジェクトでのテスト

Unity Test Frameworkを導入し、プロジェクトの `Packages/manifest.json` のルートへ以下の項目を追加します。既に `testables` がある場合は、その配列へパッケージIDを追加してください。

```json
"testables": ["com.camellian.liltoon-distance-fade"]
```

上記はmanifestへ追加する項目の断片であり、ファイル全体を置き換えるJSONではありません。

UnityのTest RunnerでEdit Modeを選び、`Camellian.DistanceFade.Tests.Editor` のテストを実行します。テストにはlilToonとVRChat SDK、NDMFも必要です。

### リポジトリの独立検証プロジェクト

以下は、`scripts/` を含むリポジトリ全体を取得し、そのルートでPowerShellを実行する手順です。配布パッケージのフォルダー単体には、これらのスクリプトや `design/`・`docs/` は含まれません。

```powershell
./scripts/Prepare-Verification.ps1 -ReferenceProject 'C:/path/to/existing-avatar-project'
./scripts/Run-Tests.ps1
```

参照元は必要な依存パッケージとUnity Test Frameworkが導入済みの、信頼できるプロジェクトを指定します。スクリプトは依存パッケージ等を読み取り、リポジトリ内の `.verification~/Unity` へ検証用にコピーします。参照元プロジェクトは変更しません。シンボリックリンク／ジャンクション等は検証スクリプトの安全性チェックで拒否されます。

テスト実行スクリプトは既定で `C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe` を使用します。別のインストール先を使う場合は指定します。

```powershell
./scripts/Run-Tests.ps1 -UnityEditor 'C:/path/to/Editor/Unity.exe'
```

Unity Editorを実行できるライセンス環境が必要です。Inspector描画の回帰テストがあるため、グラフィックスが利用できる環境で実行し、`-nographics`は指定しません。結果XMLは `.verification~/editmode-results.xml`、Unityログは `.verification~/editmode.log` に出力します。結果が古い、0件、失敗、またはUnityが異常終了した場合、実行スクリプトはエラーを返します。

検証スクリプト自体の安全性に関する回帰テストは次で実行できます。

```powershell
./scripts/Test-VerificationSafety.ps1
```

### 確認済みのテストと資料

2026-09-09のUnity 2022.3.22f1による実行で、**Edit Modeテスト54件合格・失敗0件・スキップ0件**を確認しています。非破壊適用、共有Material、部分上書き、配置・入力検証、除外とNDMF置換追跡、Prefab保存・互換性、Undo／Redo、裏面とモードの初期適用OFFによる既存値保持、初期色のRGB正規化と色単位の初期化・Undo／Redo、除外リストを展開したInspectorの描画、集計未更新時の最新設定適用を含みます。これは実アップロードや全対応環境の見た目を検証した結果ではありません。

リポジトリ全体に含まれる関連資料：

- [実装・検証記録](design/Implementation_Validation.md)
- [基本仕様・設計書](design/lilToon_DistanceFade_Bulk_Setter_Spec.md)
- [マテリアル除外機能の設計書](design/Material_Exclusion_Spec.md)
- [マルチShader拡張の設計書（未実装）](design/MultiShader_DistanceFade_Extension_Spec.md)
- [公開前レビューの記録](design/Public_Release_Review.md)

### 設定画面・変数をコードで編集する場合

| 調整内容 | 編集箇所 |
| --- | --- |
| コンポーネント追加メニュー、保存する変数・型・初期値 | [Runtime/DistanceFadeBulkSetter.cs](Packages/com.camellian.liltoon-distance-fade/Runtime/DistanceFadeBulkSetter.cs) |
| Inspectorのラベル・並び・説明・入力欄 | [Editor/DistanceFadeBulkSetterEditor.cs](Packages/com.camellian.liltoon-distance-fade/Editor/DistanceFadeBulkSetterEditor.cs)の `OnInspectorGUI`、`Field`、`ModeField` |
| 適用項目の識別、入力検証、ビルド用設定の取り込み | [Editor/SettingsValidator.cs](Packages/com.camellian.liltoon-distance-fade/Editor/SettingsValidator.cs)の `Overrides`、`SettingsSnapshot`、`Capture` |
| ShaderのProperty名、対応確認、値の書き込み | [Editor/MaterialUtility.cs](Packages/com.camellian.liltoon-distance-fade/Editor/MaterialUtility.cs) |

`Field("overrideStartDistance", "startDistance", "開始距離")` の引数は、順に「適用のbool変数」「値の変数」「表示ラベル」です。専用Inspectorのため、公開変数を追加するだけでは入力欄は表示されません。新しい適用項目を追加するときは、画面だけでなく取り込み・検証・書き込みも対応させます。

保存済み設定との互換性を保つには、既存フィールド名を変更する際に `FormerlySerializedAs` 等の移行対応が必要です。設定を変更した場合は、該当する回帰テストを実行してください。
