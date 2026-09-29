# lazyFade - NonToon

NDMFビルド時に距離フェードを一括設定します。元マテリアルは変更しません。

## 導入

1. Unity 2022.3のAvatarプロジェクトへSDK Avatars 3.10.3以上・4未満、NDMF 1.13.1以上・2未満、ShaderCore 0.1.11以上、NonToon 0.1.3以上（Packages形式） を導入します。
2. VPMでこのパッケージを追加するか、lazyFade-NonToon-0.2.1.unitypackageをインポートします。手動でPackagesへこのフォルダーを配置する方法も利用できます。
3. VRC Avatar Descriptorと同じGameObjectへ **lazyFade → lazyFade NonToon** を追加します。

同じ版のAssets版とPackages版を重複導入しないでください。旧版から更新する場合はUnityを閉じてバックアップを取り、旧ツールのフォルダーを外してから新しい版を入れます。Scene／Prefabのコンポーネント・値・参照を確認してから保存してください。

## 使い方

近距離・遠距離・強度を設定できます。適用チェックは初期状態ですべてOFFです。0 ≤ 近距離 < 遠距離 ≤ 1、強度0～1で設定します。Shader側のDistance Fadeモジュールが必要です。

「再集計」で対象を確認し、SDKからビルドします。再集計は見た目のプレビューではありません。除外マテリアルは本ツールの処理対象から外れます。非アクティブを含むMeshRenderer／SkinnedMeshRendererが対象です。AnimationClipだけが参照するマテリアルや、Shaderの機能追加は処理しません。

両版は各1個ずつ併設できます。SDK・NDMF・Shaderは同梱していません。実アバター・VR・鏡・Androidの見た目は利用環境で確認してください。

技術資料はソースリポジトリのdocs／design、公開手順はdocs/publishing.mdを参照してください。[変更履歴](CHANGELOG.md)・[MIT License](LICENSE)。
