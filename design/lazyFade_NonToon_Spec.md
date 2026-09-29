# ShaderCore＋NonToon 距離フェード対応 設計書

文書バージョン: 1.0 / 作成日: 2026-09-10  
状態: 段階Aを別パッケージ・別コンポーネントで実装。段階Bは未実装  
参照実装: lazyFade 0.1.0（lilToon専用）

## 1. 目的・範囲

ShaderCore上のNonToonで、距離フェード設定をアバタールートのlazyFadeから非破壊で一括適用する。最初はDistance Fadeモジュールを含む、出自とPropertyを確認できる生成済みShaderを対象とする。モジュールの自動追加は後続段階に分ける。

共通処理・既存コードとの対応・互換性要件は[lilToon実装参照](lilToon_Implementation_Reference.md)を参考とする。[Poiyomi設計](lazyFade_Poiyomi_Spec.md)の完成は本対応の前提条件にしない。2026-09-10のユーザー指定により、既存lilToon用とは別の`LazyFadeNonToon`を作成する方式へ変更した。同じアバタールートに各型1個を併設でき、除外・設定・プリセットも型ごとに独立する。

### 2026-09-10の実装確定事項（以下の初期案に優先）

- 配置は`Packages/com.camellian.lazyfade.nontoon`。lilToon用パッケージへのassembly参照は持たない。メニューは`lazyFade/lazyFade NonToon`。
- Runtimeに近距離・遠距離・強度、各適用チェック（初期OFF）、独立した除外配列、未対応時に停止するboolを保持する。共通ルートコンポーネントへの新規ブロック追加は行わない。
- ShaderCore 0.1.11（commit `2acb1fd4af79c3269ec04eeafee2eaeec42f1679`）とNonToon 0.1.3（commit `130bea3e6be5183b4fceb60df0062d38ef98067c`）の公式ソースを検証環境へ導入し、実Shaderで検証した。
- 公式NonToon／NonToonFurの.scshader GUIDとPackageInfoの版を確認し、公式モジュールを`SCModule.FromFile`で読む。`originalName`から`SCProperty.name`を取得し、実Shaderの型・SCModule属性・定数化なしを照合する。
- 解決された距離Propertyは`_jp_lilxyzw_nontoon_distancefade_DistanceFade`、独立強度は`_jp_lilxyzw_nontoon_distancefade_DistanceFadeStrength`。この名前を推測・固定して書き込む実装ではない。
- `ProjectSettings.GetShaderModules`には設定保存の副作用があるため、集計と識別には使用しない。生成済みShaderのPropertyと属性を読み取る。
- 元Shaderの編集、モジュール追加、外部ツールが生成した出自不明Shader、定数化の同期は対象外。Packages形式の公式導入版を対象とする。
- NonToon用27件と既存lilToon用61件の計88件が合格。値・除外・NDMF併設・GUI・失敗時の非破壊性を検証した。実描画や実アップロードの確認は残る。

実際の操作は[NonToon用README](../Packages/com.camellian.lazyfade.nontoon/README.md)、コードは同パッケージのRuntime／Editor／Testsを参照する。以下は設計時の候補・段階Bの計画も含む。

対象はNonToonのDistance Fade機能であり、ShaderCoreから生成したすべてのShaderへの汎用Property編集機能ではない。色付きフェード、距離フェード用リム、透明化、Dissolve、新規モジュールの開発は初期対象外。

## 2. 調査根拠と対応版

| 根拠 | 今回確認した内容 | 扱い |
| --- | --- | --- |
| NonToonのmainにあるpackage.json | `jp.lilxyzw.nontoon` 0.1.3、Unity 2022.3、ShaderCoreへの依存宣言 | 文書上の調査基準。全互換性の保証ではない |
| Shader-Coreのmainにあるpackage.json | `jp.lilxyzw.shadercore` 0.1.11 | 初期API調査の候補版 |
| NonToonのDistanceFade/properties.hlsl | 距離Vectorと独立した強度 | 下記の項目設計の根拠 |
| NonToonのDistanceFade/phase_postpixel.hlsl | headDepthに基づくRGBの黒方向への補間 | 距離基準・表示名をlilToonと分ける根拠 |
| 旧統合設計の2026-09-08調査記録 | モジュールID `jp.lilxyzw.nontoon.distancefade` | 採用版の.scmoduleで再確認する候補ID |

manifestは[NonToon公式](https://raw.githubusercontent.com/lilxyzw/NonToon/main/package.json)、[ShaderCore公式](https://raw.githubusercontent.com/lilxyzw/Shader-Core/main/package.json)を2026-09-10に参照した。可変のmainを参照しており、リリースタグ／コミットを固定した検証ではない。実装開始時に採用ソースの識別子・ハッシュ、Unity・NDMF・Core・NonToonの組合せを記録する。設計初稿時点では追加Shaderは未導入だった。現在の検証環境は冒頭の実装確定事項を参照する。

## 3. lilToonとの意味の違い

| 観点 | 現行lilToon | NonToonで設計する動作 |
| --- | --- | --- |
| 距離 | `_DistanceFade.x/y`を開始／終了として部分上書き | モジュール内の距離Vector.x/yを近距離境界／遠距離境界として扱う |
| 強度 | 距離Vector.z | 独立した`_DistanceFadeStrength`。距離Vector.z/wは保持 |
| 色・リム | 色、リム色、リム指数を設定 | 確認したモジュールには対応項目なし。lilToonの色を転記しない |
| 裏面・モード | Vector.w、独立したMode | 確認したモジュールには対応項目なし。UIへ架空の設定を追加しない |
| Shader識別 | 公式Shader名一覧と限定したカスタム名規則 | NonToon由来・生成構成・モジュール・対応版から識別 |
| 設定名 | Materialに渡す既知のProperty名 | モジュール宣言名と生成後の最終名を区別 |

確認した描画処理は`vertex.headDepth`を使い、近い側で強度に応じてRGBを黒へ近づける。Alphaを書き換える距離透明化ではない。headDepthの生成・単位・VRや鏡での基準は実装時の確認事項であり、単純なカメラからのユークリッド距離とは断定しない。[NonToonの描画処理](https://raw.githubusercontent.com/lilxyzw/NonToon/main/Shaders/Modules/DistanceFade/phase_postpixel.hlsl)

## 4. 保存データと入力検証

`NonToonSettings`を新しいシリアライズ可能データとして用意する案とする。名称は実装案で、既存APIではない。`enableNonToon`はfalse、以下の適用チェックは全OFF。RuntimeからShaderCore Editor型を参照しない。

| 変数案 / 表示 | モジュール内の宣言 | 入力と仮初期値 | 適用時の規則 |
| --- | --- | --- | --- |
| `nearDistance` / 近距離境界 | `_DistanceFade.x` | float、0.01 | xだけ上書き |
| `farDistance` / 遠距離境界 | `_DistanceFade.y` | float、0.1 | yだけ上書き |
| `strength` / 強度 | `_DistanceFadeStrength` | float、0 | 独立Propertyへ上書き |

これらは確認した宣言の初期値を採用する設計である。Vectorの初期z/wは0だが、既存Materialのz/wを初期化してよいという意味ではない。[NonToonのProperty宣言](https://raw.githubusercontent.com/lilxyzw/NonToon/main/Shaders/Modules/DistanceFade/properties.hlsl)

各値に`overrideNearDistance`／`overrideFarDistance`／`overrideStrength`を設ける。適用ONの入力は有限値とし、元Materialから保持する値と合成した最終ペアを`0 <= near < far <= 1`、強度を0～1で検証する。これは初期提供範囲をUI属性に合わせて限定する設計判断であり、Shaderの数学的な絶対上限を示すものではない。

片側だけの上書きでも最終ペアを検証し、同値・逆順・範囲外を自動並べ替えや無言のClampで直さない。ON入力の不正と最終ペアの不正は共通設計のビルドエラーとする。lilToonの開始0.18／終了0.01をそのままコピーするとこの条件を満たさないため、自動変換は提供しない。

## 5. 識別と最終Property名の解決

1. 共通除外と対象ON／適用ONの判定を先に行う。
2. MaterialからShaderを取得し、対応するCore／NonToon版を特定する。
3. 生成元.scshader、Importerまたは対応版APIのメタデータからNonToon由来と選択モジュールを確認する。
4. 対象ShaderにDistance Fadeモジュールが実際に含まれることを確認する。インストール済みだけでは対象にしない。
5. モジュールIDから当該モジュールの最終Property名を解決し、距離Vectorと強度の型・存在を検証する。
6. 読み取った既存値と上書き設定から変更計画を作る。

ShaderCoreはモジュール固有IDで変数名の衝突を回避する仕組みを持つ。したがって宣言名をそのままSetVectorへ渡す実装や、推測した接尾辞の文字列連結は採用しない。対応版API／生成情報で解決できない場合は未対応とする。[ShaderCore公式の構成説明](https://github.com/lilxyzw/Shader-Core)

解決結果には元Shader、モジュール識別子、採用版、最終名、型、定数化状態を含める。生成後は再解決し、古いProperty IDを流用しない。同名の宣言を持つ別モジュールに書き込まない。

## 6. ビルドとモジュール生成

### 段階A: 組み込み済みモジュール

`NonToonDistanceFadeAdapter`と`ShaderCoreIntegration`をEditor側に設ける案とする。前者は3項目の計画と部分上書き、後者は対象版の出自・名前・生成状態の解決を担当する。共有する走査、除外、Material複製、保存と参照確定は現行コードを基にする。

解決したPropertyが有効な構成だけ複製へ適用する。Shaderやキーワードの変更が不要な場合は現行の非破壊経路を利用する。定数化などによりSetFloatだけでは反映しない構成は、その同期経路が完成するまで未対応とする。ShaderCoreにはSCConstValueと生成キーワードを用いる定数化の仕組みがあるが、本モジュールのすべての値が定数化されるとは仮定しない。[ShaderCore公式の定数化説明](https://github.com/lilxyzw/Shader-Core)

モジュール不足・出自不明・最終名未解決は、既定でMaterial全体を警告してスキップする。FailBuildでは停止。通常の非対象Shaderや手動除外は警告しない。

### 段階B: 不足モジュールの自動追加

`addMissingModule`は初期false。非破壊生成APIと後始末を確認するまで操作可能にしない。次をすべて満たした対応版だけで提供する。

- ビルド用に元構成を複製し、既存モジュール・順序・設定を保ってDistance Fadeだけを追加できる。
- 元.scshader・.scmodule・Importer・元Material・生成元Shaderの変更や再インポートを要求しない。
- 出力Shaderの参照するinclude等も、生成物の寿命とビルド完了までの利用を保証できる。
- Shader生成後にProperty名を再解決し、必要な定数化・キーワード同期を複製側だけに実行できる。
- NDMFの保存・削除管理へ正しく接続し、連続ビルドや生成失敗で恒久的なアセットを残さない。

具体的なAPI型・メソッド名・保存経路は未確定。文字列生成でShaderを書き換える代替を暗黙に採用しない。段階Bを未提供のまま段階Aをリリースしてよい。

現在のTTT後・AAO前という適用位置を基準に、ShaderCoreを操作する外部ツールの最終構成確定後に計画する。Module Override等の外部生成物は出自を追跡できる場合だけ対象化する。外部ツールの必須依存化や未確認Plugin IDの順序指定は行わない。

## 7. Inspector・プリセット・診断

追加対象の有効化後、基本設定に近距離境界→遠距離境界→強度を並べる。左端の適用チェックには文字ラベルを付けず、OFFは既存値保持とツールチップで説明する。強度0を書き込むことと、適用チェックOFFを区別する。

対象用のプリセット欄を3項目の先頭に設ける。`NonToonLazyFadeLilToonPresets.cs`に名前と3値の定義を置き、3件の編集枠を用意する案とする。初期の値は本書の仮初期値を使い、描画検証前に推奨設定として案内しない。選択時に3値を即時上書きし、チェック・追加対象の有効化・共通除外・他Shaderの設定は保持する。lilToon用の6値プリセットを表示しない。

共通除外は1段の標準配列リスト。高度な設定には失敗ポリシーと、提供可能になった段階Bの明示オプションを置く。モジュール状態・導入版・未対応理由は読み取り専用表示とし、集計の操作だけで生成しない。

診断は「モジュール未組み込み」「生成元を解決できない」「距離の最終ペアが不正」「生成後のPropertyが不一致」など、利用者が原因を区別できる内容にする。成功・部分適用・未対応・除外を別件数で示す。

## 8. 受入試験と実装開始条件

共通試験C01～C10に加え、次を実施する。すべて実施予定であり、合格済みではない。

| ID | 試験 | 期待結果 |
| --- | --- | --- |
| N01 | モジュール組み込み済み／未組み込み／Coreだけ導入 | 対応状態を正しく区別し、誤適用なし |
| N02 | 距離片側上書きと既存z/wが非ゼロ | 保持値と合成して検証し、z/wは不変 |
| N03 | lilToonとNonToonが同じRendererに存在 | Vector.zと独立強度を混同しない |
| N04 | 同名宣言の別モジュール・生成済みShader | 対象モジュールの最終名だけ変更 |
| N05 | 同距離・逆順・範囲外・NaN・Infinity | 書き込み開始前に診断、暗黙補正なし |
| N06 | 定数化／キーワードを持つ構成 | 対応経路のみ変更し、未対応なら成功扱いにしない |
| N07 | モジュール自動追加OFF／ON、生成失敗 | OFFはスキップ、対応ONだけ生成、元構成は不変 |
| N08 | 外部ツール生成とMA／TTT／AAOを併用 | 最終構成へ適用され、最適化後にも値・描画が残る |
| N09 | headDepthの境界・VR両眼・鏡・カメラ | 距離基準と見た目を記録し、READMEの説明と一致 |
| N10 | 3値プリセット・未更新集計・Inspector再表示 | 値だけ更新、他対象と保存済み設定は不変 |

着手時に確定する項目は、対応版のモジュールID、出自判定API、最終名解決API、headDepthの生成と単位。段階Bではビルド専用生成・定数化同期・生成物寿命のAPIも必須。型やPropertyが見つかっただけで対応済みとせず、固定した版の生成後出力と実描画まで確認する。
