# Poiyomi Proximity Colorによる距離フェード対応 設計書

文書バージョン: 1.0 / 作成日: 2026-09-10  
状態: 未実装の設計。Property候補と動作保証を区別する  
参照実装: lazyFade 0.1.0（lilToon専用）

## 1. 目的・範囲

Poiyomi ToonのProximity Colorを、lazyFadeのルート設定から非破壊で一括適用する。距離に応じた色変化という用途を扱うが、lilToonの色・強度・リムと同一の描画を保証する機能にはしない。

共通の配置・除外・複製・集計・互換性は[lilToon実装参照](lilToon_Implementation_Reference.md)に従う。[ShaderCore＋NonToon設計](lazyFade_NonToon_Spec.md)とは独立に着手できる。既存コンポーネントへPoiyomi用データとEditorアダプターを追加する案とする。

対象は対応版として固定したPoiyomi Toonのバリアントとロック経路に限定する。Poiyomi Pro、未確認の派生Shader、透明化・Dissolve、独立したRim Light、AnimationClipの修正、元Materialのロック解除は初期対象外。

## 2. 調査基準と未確認事項

2026-09-10に参照した[公式Proximity Color説明](https://www.poiyomi.com/special-fx/proximity-color)は掲載版10.0。カメラ距離による色変化、Pixel／Object位置、近距離・遠距離の色と距離、裏面色の扱いを確認した。掲載版と実際に導入するパッケージ版を区別する。

旧統合設計は2026-09-08にPoiyomi Toon 9.3.64の`_PoiyomiShaders/Shaders/9.0/Toon/Poiyomi Toon Early Outline.shader`を調査したと記録している。本書はそのProperty候補を引き継ぐが、今回はその旧版ソースを再照合していない。10.xの数値enum・Shader名・Property・APIへ自動的に適用できる表ではない。

今回のlilToon検証プロジェクトにはPoiyomiを導入していない。着手時にバージョン、配布元、タグ／コミットまたはソースハッシュ、Shaderバリアント、ロック実装、NDMF・SDK版を固定して記録する。現時点の対応済みバージョンはない。

## 3. lilToonとの差分

| 観点 | 現行lilToon | Poiyomiで扱う意味 |
| --- | --- | --- |
| 色 | フェード先の色 | 近距離・遠距離でベース色に乗算する色 |
| 距離 | 開始／終了。既存実装は順序を自動補正しない | Min／Max。初期対応は最終Min < Maxに限定 |
| 強度・リム | 独立した基本項目 | この対応では同等項目を作らない |
| 位置基準 | 頂点／座標の既存enum | Pixel Position／Object Position。整数値は採用版で解決 |
| 裏面 | 距離Vector.wのbool | Force BackFace Color。Cullの条件も関係する |
| 複製後の書き込み | 既知Propertyへ直接適用 | ロック後にPropertyや機能が生きていることまで必要 |

Min／Max色はそれぞれ距離の両端での乗算色で、裏面色の機能にはCull Offが必要と説明されている。この設計ではCull・描画モードを自動変更しない。[公式の色・裏面設定](https://www.poiyomi.com/special-fx/proximity-color)

lilToonの暗いフェード色をMin Colorへ単純コピーすると異なる見た目になり得る。`warm/cold/none`の6値を変換する機能は提供せず、Poiyomi独自の設定名とプリセットを使う。

## 4. 保存データとProperty候補

`PoiyomiSettings`は外部Editor型を含まないシリアライズ可能データとして追加する案。`enablePoiyomi`はfalse、7項目それぞれの適用チェックも初期OFFにする。

次の表のProperty名と0／1の対応は旧9.3.64調査からの候補であり、採用版の宣言・描画処理で再確認してから実装する。初期値は本ツールの提案値であって、全Poiyomi版の公式初期値を示さない。

| 変数案 / 表示 | Property候補 | 保存型 | 仮初期値 |
| --- | --- | --- | --- |
| `effectEnabled` / Proximity Colorを有効化 | `_FXProximityColor` | bool→float | false |
| `positionMode` / 距離の基準 | `_FXProximityColorType` | int | 1（旧調査ではPixel、0はObject） |
| `minDistance` / 近距離境界 | `_FXProximityColorMinDistance` | float | 0 |
| `maxDistance` / 遠距離境界 | `_FXProximityColorMaxDistance` | float | 1 |
| `minColor` / 近距離の乗算色 | `_FXProximityColorMinColor` | Color | black、Alpha 1 |
| `maxColor` / 遠距離の乗算色 | `_FXProximityColorMaxColor` | Color | white、Alpha 1 |
| `forceBackfaceColor` / 裏面に近距離色を使う | `_FXProximityColorBackFace` | bool→float | false |

`overrideEffectEnabled`等の7つのチェックを別に持つ。効果の有効化行は左の「上書きするか」と右のbool値を分ける。対象ONだけで機能有効化や既存値変更を行わない。機能ONを明示適用する場合は、保持される距離・色・位置基準も含めて成立条件を検証する。

### 入力と最終値

- ON入力のfloatとColor全成分は有限値とする。ColorのAlphaも保存値検証の対象。
- 対象Materialの保持値と合成した最終距離ペアを`0 <= Min < Max`に限定する。片側上書きも検証し、同距離・逆順はビルドエラー。これは初期提供範囲の設計判断である。
- 位置基準の既存未知値は保存・表示で保持する。明示適用や機能ON時に採用版で有効と確認できなければ、未対応として扱う。
- HDRの入力可否と有効範囲は採用版の宣言・描画から決める。lilToonのColorUsageを無条件にコピーせず、任意保存値も勝手にClampしない。
- 色のAlphaを距離透明化として説明しない。旧調査ではRGBが描画対象だったため、採用版でAlphaの用途を確認し、用途がない場合は保持する設計にする。RGB適用とAlpha保持を分けて実装する。

旧調査には色のTheme Indexも存在すると記録されている。Themeを勝手に0へ戻さず、指定RGBが実際に使われる構成かを採用版で確認する。解決できないTheme構成では色適用を未対応と診断し、機能全体の成立が保証できない場合はMaterial全体をスキップする。

## 5. 対象識別とロックへの対応

Shader名の部分一致やHasPropertyだけでPoiyomiと判定しない。検証版の元Shader、バリアント、生成Shaderの起源、ロック状態、実際のProperty名、機能有効状態を読み取り専用で検出する。分からない状態を未ロックと見なさない。

公式資料は、ロックによる静的値・未使用機能の最適化、アップロード時のロック、アニメーション用Propertyの扱いを説明している。このためSetFloatの成功だけでは適用完了としない。[公式のLocking and Animation](https://www.poiyomi.com/general/locking)

| 状態 | 初期設計の扱い |
| --- | --- |
| 未ロックで、最終ロックまで検証した版 | 複製に設定し、後段ロックを含む最終出力まで検証 |
| ロック済みで、必要機能・動的Propertyが存在 | 最終名と書き込み可能性を確認した項目だけ計画。機能を後から有効化できるとは仮定しない |
| ロック済みで、静的値変更が必要 | 非破壊復元・再ロック経路を実装するまで未対応 |
| 必要機能削除済み、起源や状態が不明 | Material全体を未対応として警告／FailBuild |

段階Aは、確実に値を反映できる構成だけを提供する。段階Bで復元情報のあるビルド用複製の再ロックを追加する。段階Bが未完成なら利用者に元Materialの自動解除を要求する実装へ置き換えず、対象外と明記する。

### アニメーションと名前の保持

公式のRenamed Animated PropertyはMaterial名に基づく名前を扱う。現行`MaterialCloneCache`の名前への`(Distance Fade)`追加をPoiyomiへそのまま適用しない。元Material名と既存のリネーム情報を計画に含め、複製後も既存のAnimated／Renamed設定とバインディングを維持する。[公式のProperty名とアニメーション](https://www.poiyomi.com/general/locking)

A／RAフラグの自動付け直しやAnimationClip書き換えは行わない。既存名の保持だけで十分とは仮定せず、ロック後の最終名も検証する。アニメーションがビルドで設定した値を再生時に上書きする可能性は、現行lilToonと同様に説明する。

## 6. 実装の責務と順序

| 新規構成案 | 責務 |
| --- | --- |
| `PoiyomiDistanceFadeAdapter` | 対応判定、7項目の取り込み、保持値との合成、変更計画、複製への書き込み |
| `PoiyomiIntegration` | 採用版のロック状態・最終名・復元情報を解決し、対応する生成APIへ接続 |
| `PoiyomiLazyFadeLilToonPresets` | 対象用プリセットの定義と値コピー |
| 共通のPass・Cache・Summary | 走査、除外、計画の競合防止、名前保持方針、保存と参照確定、診断 |

これらは型名の案であり、外部製品が提供するAPI名ではない。外部Editor APIへの参照は任意アドオンassemblyなどに隔離し、未導入時のasmdef解決も試験する。Poiyomi本体を同梱しない。

ビルド順は、Material構成確定→再走査・除外→最終値の計画→複製と設定→必要なロック／生成→生成物検証→保存と参照確定とする。後段のSDKコールバックが再ロックする場合は、その後の最終出力検証も必要。現行TTT後・AAO前という配置だけでロック順序が保証されるとは扱わない。

段階Bは、元Material／元Shader／ロック情報を変更せず、複製だけの復元と再生成を完結できることを条件とする。生成Shader、依存するinclude、作業用パスの寿命・保存・失敗時の後始末まで確認する。ビルド中の恒久的なプロジェクト変更を要求するAPIは採用しない。対応する非破壊APIとSDKコールバック順は未確定である。

## 7. Inspector・プリセット・診断

Poiyomi用ブロックを明示的に有効化したとき、基本設定の先頭にプリセット、その下に近距離境界→遠距離境界→近距離色→遠距離色→距離の基準を並べる。位置基準のプルダウンはPixel Position／Object Positionの意味を使い、lilToonの「頂点／座標」と同一と説明しない。

機能有効化と裏面色はオプションとして折りたたみ、上書きチェックとbool値を分ける。機能OFFの既存Materialへ基本値だけ設定しても効果は有効にならないことを、ブロック内の案内に示す。Cull Offでない場合は裏面色の条件を表示し、Cullを書き換えない。

プリセットは名前と基本5値を編集する3件の枠を用意する案とする。仮初期値は第4章の基本5値。選択時に5値だけ即時上書きし、適用チェック・効果有効化・裏面色・共通除外・他Shaderの設定は保持する。lilToonと同じ保存・個別指定への復帰規則を使い、専用の確定ボタンは設けない。RGBだけ適用する対応版では、プリセットによる色更新もAlphaを保持する。

共通除外リストは1段だけ。高度な設定には未対応時の失敗ポリシーを置く。ロック状態と対応可否を表示し、集計中に解除や再ロックを行わない。診断は「Property不足」にまとめず、「ロックで静的化」「機能が削除済み」「Themeで指定色を保証できない」「名前を保持できない」を区別する。

## 8. 受入試験とリリース条件

共通試験C01～C10に加え、次を実施する。すべて計画であり、追加Shaderの動作実績ではない。

| ID | 試験 | 期待結果 |
| --- | --- | --- |
| P01 | 対応バリアント・別版・紛らわしいShader名 | 固定した対応範囲だけ選択 |
| P02 | 近距離・遠距離色とPixel／Object位置 | 採用版の最終名・enum・乗算の意味と描画が一致 |
| P03 | 距離片側上書き、同値・逆順・非有限値 | 保持値との最終ペアを検証、部分書き込みなし |
| P04 | 効果OFFの保持／明示ON／明示OFF | 基本値変更と機能有効化を混同せず、削除機能の復活を偽らない |
| P05 | Theme・Alpha・HDR・Cullの組合せ | 保証範囲に一致、未対応は診断、無関係設定を保持 |
| P06 | 未ロック→アップロード時ロック | 最終出力にも効果が残る |
| P07 | ロック済み静的値・動的値・機能削除 | 対応経路だけ適用、未対応は警告／停止 |
| P08 | A／RA、同名Material、既存アニメーション | 元名・生成名・フラグ・Clipを保持 |
| P09 | 再ロック失敗・保存失敗・複数連続ビルド | 元アセット不変、生成物とキャッシュ混入なし |
| P10 | MA／TTT／AAO、SDKコールバックを併用 | 順序ログと最終描画で設定保持を確認 |
| P11 | 基本5値プリセット・Inspector・集計 | 値の即時反映、オプション保持、集計による変更なし |

実装開始時には、採用版ソースによる第4章の表の確定、Shader／ロック識別方法、名前保持、非破壊ロックAPI、SDKコールバック順を優先調査する。Material値の比較だけでリリース可能とは判定せず、VR・鏡・カメラ・Play Mode・実ビルドで検証した組合せを対応表へ記載する。未実装の再ロック経路は明示的に非対応として段階Aを公開できる。
