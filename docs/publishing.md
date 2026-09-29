---
layout: default
title: "VPM・GitHub Releaseの公開手順"
---

# VPM・GitHub Releaseの公開手順

2つのパッケージを1つのVPMリポジトリから配布します。公開URLと公開用メールを指定すると、ZIP・VPM一覧・SHA256を生成できます。認証トークン、独自サーバー、UnityライセンスをGitHubへ登録する必要はありません。

## 初回公開：必要な作業

1. GitHubに公開リポジトリ（例：`所有者/lazyFade`）を作成します。README等の初期ファイルは作らず、このソースを配置します。
2. リポジトリルートで**PowerShell 7.4以上**を開き、次の2項目を実際の公開情報に置き換えて実行します。

   ```powershell
   ./scripts/Build-VpmRepository.ps1 -Repository 'YOUR-ACCOUNT/lazyFade' -PublicEmail 'public-contact@example.com'
   ```

   `PublicEmail`はZIPと公開一覧に入る連絡先です。実際に使用する公開用アドレスを指定してください。GitHub情報は大文字小文字も公開URLに合わせます。
3. 安全チェックが合格したことを確認してからソースをGitHubへpushします。`docs/index.json`を含め、`artifacts/`、`.verification~/`、元のWord資料は含めません。Git未初期化なら以下を使用できます。

   ```powershell
   git init -b main
   git add README.md LICENSE .gitignore .gitattributes Packages scripts docs design release
   git diff --cached --stat
   ./scripts/Test-PublicationSafety.ps1
   git commit -m 'Prepare lazyFade 0.2.0 release'
   git remote add origin https://github.com/YOUR-ACCOUNT/lazyFade.git
   git push -u origin main
   ```

   既存Gitリポジトリではinitとremote addを繰り返す必要はありません。
4. GitHubの **Releases → Draft a new release** でタグ **v0.2.0** を作り、本文に`artifacts/0.2.0/RELEASE_NOTES.md`を使います。以下を添付して公開します。

   | 添付ファイル | 用途 |
   | --- | --- |
   | `lazyFade-lilToon-0.2.0.unitypackage` | lilToon版の手動導入 |
   | `lazyFade-NonToon-0.2.0.unitypackage` | NonToon版の手動導入 |
   | `lazyFade-lilToon-0.2.0.zip` | lilToon版のVPM配布 |
   | `lazyFade-NonToon-0.2.0.zip` | NonToon版のVPM配布 |
   | `SHA256SUMS.txt` | 4アーカイブのハッシュ |

   GitHubが自動生成するSource code (zip)はVPM用ZIPではありません。ファイル名・タグを変えると一覧のURLと一致しなくなります。
5. GitHubの **Settings → Pages → Deploy from a branch → main /docs** を選択します。公開後、生成コマンドが表示した`https://YOUR-ACCOUNT.github.io/lazyFade/index.json`を開き、JSONと両ZIPのダウンロードを確認します。`OWNER.github.io`形式のリポジトリではパス部分を自動省略します。
6. VCCの **Settings → Packages → Add Repository** にそのindex.json URLを登録します。依存リポジトリも[導入ガイド](installation.html)に従って追加し、バックアップ済みAvatarプロジェクトで導入・ビルドしてください。

これで**URLから追加できるCommunity Repository**になります。VRChatのCurated登録・公式審査への申請とは別です。

## 生成処理の仕様

入力は両版のpackage.jsonとRuntime／Editor／README／CHANGELOG／LICENSEです。Testsと依存は除外し、ZIP直下にpackage.jsonを置きます。

元manifestに未確定URL・仮メールは記録しません。生成ZIPのmanifestへurl、repo、documentationUrl、changelogUrl、author.emailを追加します。vpmDependenciesは保持し、zipSHA256は自己参照を避けて一覧だけに追加します。

`docs/index.json`はname／author／id／url／packagesを持ち、`packages.<ID>.versions.<version>`へ公開manifestを格納します。Pagesで静的配信するため実行サーバーや秘密鍵は不要です。

同一入力は固定ZIP時刻で再生成できます。同一版の異なる内容への上書きや、既存一覧のID・URL変更は拒否します。過去バージョンの一覧を保持し、新版を追加します。公開済みZIP・タグ・過去版を削除・上書きしないでください。

カスタムドメインの公開先が確定している場合は指定できます。

```powershell
./scripts/Build-VpmRepository.ps1 -Repository 'YOUR-ACCOUNT/lazyFade' -PublicEmail 'public-contact@example.com' -ListingUrl 'https://packages.example.com/index.json'
```

この引数はDNSやPagesのカスタムドメインを設定しません。

## 次回更新

1. 両版のpackage.jsonを同じ新バージョンにし、CHANGELOGと`release/<version>.md`を更新します。
2. [開発手順](development.html)のUnityテスト、安全テスト、両unitypackageの作成を行います。
3. 同じ公開情報でBuild-VpmRepository.ps1を実行します。
4. 新版Releaseを先に公開し、ZIPを取得できることを確認してから更新一覧をpushします。過去Releaseを残します。

## 失敗時

| 症状 | 確認 |
| --- | --- |
| 一覧が404／HTML | Pagesがmain /docsか、index.jsonがcommit済みか、Pagesの公開完了状況 |
| ZIPが404 | Releaseが公開済みか、タグがv付きか、ファイル名が一致するか |
| 依存解決失敗 | NDMF・lilxyzwのリポジトリ登録と対応版の存在 |
| 同版の内容不一致 | 公開済みなら版を上げる。未公開の試作なら既存出力・一覧を保管して再生成 |
| class／assembly重複 | [移行手順](migration.html)で旧新・Assets／Packagesの重複を解消 |

仕様：[VRChat VPM Packages](https://vcc.docs.vrchat.com/vpm/packages/)、[VPM Repos](https://vcc.docs.vrchat.com/vpm/repos/)、[独自一覧の公開](https://vcc.docs.vrchat.com/guides/create-listing/)、[Pages公開元設定](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)。
