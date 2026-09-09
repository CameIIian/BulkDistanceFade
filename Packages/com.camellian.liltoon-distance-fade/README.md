# lilToon Distance Fade Bulk Setter

アバタールートの設定を、NDMFビルド時に全lilToon Materialへ適用するUnity Editor拡張です。元のMaterial、Prefab、Textureは変更しません。

## 導入

Unity 2022.3のVRChat Avatarプロジェクトへ、VRChat Avatar SDK、NDMF、lilToonを先に導入してください。その後、このフォルダーをプロジェクトの`Packages/com.camellian.liltoon-distance-fade`へコピーしてください。Unity Package Managerの「Add package from disk」で本フォルダーの`package.json`を選ぶこともできます。後者ではVPM依存パッケージは自動導入されません。

VPM用のメタデータを含みますが、公開VPMリポジトリへの登録は行っていません。

## 使い方

1. Hierarchyで**VRC Avatar Descriptorと同じアバタールートGameObject**を選びます。
2. Add Componentから`lilToon/Distance Fade Bulk Setter`を追加します。
3. 距離、強度、色等を設定し、変更したくない項目は「適用」をOFFにします。
4. 処理したくないMaterialを「除外マテリアル」に追加します。同じMaterialを使うすべてのスロットが除外されます。
5. 「集計を更新」で編集時の対象件数・除外件数と設定エラーを確認します。
6. NDMFが処理するPlay Mode移行またはアバタービルドで結果を確認します。

**初期強度は0です。** 初期状態ではフェードは実質無効で、適用ONなら元Materialの強度も0へ上書きします。開始距離0.1／終了距離0.01の順序は意図した初期値です。

設定は1アバター1個までです。子オブジェクトへの配置や重複は、無効コンポーネントも含めビルドエラーになります。正しい位置でコンポーネントのチェックをOFFにすると、Materialは変更されません。設定コンポーネントはビルド出力から除去されます。

## 処理

- MeshRenderer／SkinnedMeshRendererを非アクティブ・無効Rendererも含めて走査します。
- MA本体・MA late transformの完了後、Optimizing段階のTTT後・AAO前に適用します。TTT 1.0.1の後段適用にも対応するため、この位置を使用します。
- 元Materialごとに1個の複製を生成し、対象Slot間の共有を保持します。他ツール由来の一時Materialも複製します。
- 除外Materialは複製・変更・置換せず、不足Propertyの警告も出しません。除外リストの空欄・重複・未使用Materialは無視し、同名の別Materialは区別します。
- `_DistanceFade`は適用ONの要素だけを変更し、OFFの値を維持します。
- Strict ONではlilToon 2.3.2由来の52個の公式Shader名を使用します。OFFではパス要素が`lilToon`で始まるカスタム名も候補にします。どちらでも各Propertyの存在確認を行います。
- 不足PropertyはまとめてMaterialごとに1回警告します。非対応項目以外は適用します。
- NaN／Infinityは書き込み前に拒否します。フレネル指数の有限値は0.01～50に補正します。
- モードは0（頂点）、1（オブジェクト位置）を含むint値として編集・保存し、未知の値も保持します。

## 制限

Inspector集計は読み取り専用であり、Sceneの見た目を即時変更するPreviewではありません。MA／TTT処理後は対象件数が変わる場合があります。

他ツールがMaterialを置換した場合、NDMFへ置換元が登録されていれば除外を引き継ぎます。未登録の置換や複数Materialの統合では、指定した元Materialを追跡できない場合があります。除外は本ツールの処理だけに適用され、他ツールによる変更を止めるものではありません。除外件数は実際に走査したMaterialとスロットの数で、リストに登録した件数ではありません。

AnimationClipのMaterialプロパティやMaterial差し替えキーは変更しません。アニメーションによって適用した初期値が上書きされる場合があります。Shaderキーワードや描画モードも変更しないため、Propertyが存在してもShaderの機能削減設定によって距離フェードが描画されない場合があります。

## 開発・検証

API照合の基準はUnity 2022.3.22f1、VRChat SDK 3.10.3、NDMF 1.13.1、lilToon 2.3.2です。依存範囲内の全バージョン・全プラットフォームの動作保証ではありません。MA／TTT／AAOは任意で、直接のassembly参照はありません。実施した試験と未実施項目はリポジトリの`docs/Implementation_Validation.md`に記録します。

Unity Test RunnerでEdit Modeテストを実行するには、導入先の`Packages/manifest.json`に次を追加します。

```json
"testables": ["com.camellian.liltoon-distance-fade"]
```

既存の`testables`がある場合は配列に追加してください。Unity Test Frameworkも必要です。

## ライセンス

本パッケージのコードはMIT Licenseです。依存パッケージはそれぞれのライセンスに従います。
