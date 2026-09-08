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

## 残る手動検証・リリース条件

- MAでの衣装追加、TTTでのMaterial差し替え、AAO最適化を含む実アバターでの最終出力確認。
- MA／TTT／AAOの全8構成の互換性試験と各採用版の実行ログ確認。
- Play Mode移行と実際のVRChatアップロード、Shader機能削減設定、同距離等の境界値の見た目。
- Inspectorの目視確認、Prefab override、Undo／Redo、アニメーションで上書きされる場合の挙動。
- Android等、Windows Editor以外の対象環境。

これらは自動Edit Modeテストの合格だけでは確認済みとしない。依存バージョン範囲の宣言は、範囲内の全組合せの検証完了を意味しない。
