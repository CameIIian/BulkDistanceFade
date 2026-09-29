# lazyFade

VRChatアバターの距離フェードを、NDMFビルド時にまとめて設定するUnity Editor拡張です。元マテリアルは変更しません。

| 版 | 必要な環境 | 配布ファイル（v0.2.0） |
| --- | --- | --- |
| lilToon | Unity 2022.3、SDK Avatars、NDMF、lilToon 2.3.2以上・3未満 | `lazyFade-lilToon-0.2.0.unitypackage` |
| NonToon | Unity 2022.3、SDK Avatars、NDMF、ShaderCore 0.1.11、NonToon 0.1.3 | `lazyFade-NonToon-0.2.0.unitypackage` |

## 導入・使い方

1. 必要な依存パッケージを導入し、使う版のunitypackageを **Assets → Import Package → Custom Package** からインポートします。VPMで導入する場合は[導入ガイド](docs/installation.md)を参照してください。
2. **VRC Avatar Descriptorと同じGameObject** に、Add Componentの **lazyFade → lazyFade lilToon / lazyFade NonToon** を追加します。
3. 上書きする項目のチェックをONにし、値を設定します。**NonToon版の適用チェックは初期状態ですべてOFF**です。
4. 「再集計」で対象を確認し、VRChat SDKからビルドします。編集画面のマテリアルは変わりません。

両版は各1個ずつ併設できます。同じ版のunitypackageとVPM版は重複導入しないでください。旧版からの更新は[移行手順](docs/migration.md)を確認してください。

## 詳細

- [設定項目](docs/settings.md)・[NonToonガイド](docs/nontoon.md)・[トラブルシューティング](docs/troubleshooting.md)
- [開発・テスト](docs/development.md)・[技術仕様](design/README.md)
- [VPM公開手順](docs/publishing.md)・[Release作成](release/README.md)・[安全チェック](docs/security.md)

lilToon／NonToon公式の機能ではありません。Poiyomiは未対応です。ライセンスは[MIT](LICENSE)。
