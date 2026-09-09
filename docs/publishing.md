---
layout: default
title: "GitHub Pagesの公開・更新"
---

[ホーム](index.html) · [導入](installation.html) · [使い方](usage.html) · [設定項目](settings.html) · [対応環境](environment.html) · [注意事項](notes.html) · [トラブルシューティング](troubleshooting.html) · [Build / Test](development.html)

---

# GitHub Pagesの公開・更新

GitHub Pages向けの利用者ドキュメントは `docs/`、設計書・検証記録は `design/` に格納しています。このページはドキュメントを公開・保守する方向けの手順です。

## GitHubで公開する

1. このリポジトリをGitHubへ配置し、公開に使うブランチへ `docs/` を含めます。
2. リポジトリの **Settings → Pages** を開きます。
3. **Build and deployment → Source** で **Deploy from a branch** を選びます。
4. 公開するブランチ（例：`main`）とフォルダー **/docs** を選択して保存します。ルート `/` は選びません。
5. Pagesのビルド・デプロイ結果を確認し、Settings → Pagesに表示されるURLを開きます。失敗時はActionsに表示されるログを確認してください。

ブランチと `/docs` を公開元にする設定は[GitHub公式の公開元設定手順](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)に従っています。公開URLやブランチ名はリポジトリに依存するため、原稿へ特定の所有者名・リポジトリ名を固定していません。

このフォルダーを作成しただけではサイトは公開されません。GitHub側の設定・push・公開作業は別途必要です。リポジトリやアカウントでPagesが利用可能かもGitHub側で確認してください。

## ファイル構成

| ファイル | 用途 |
| --- | --- |
| `index.md` | 概要・特徴・ガイド一覧 |
| `installation.md` | 導入 |
| `usage.md` | 使い方 |
| `settings.md` | 設定画面の全項目 |
| `environment.md` | 対応環境と検証範囲 |
| `notes.md` | 注意事項 |
| `troubleshooting.md` | 症状別の対処 |
| `development.md` | Build / Test |
| `license.md` | パッケージのライセンス |
| `publishing.md` | この公開・更新手順 |
| `_config.yml` | サイト名・言語・テーマ・除外設定 |
| `Gemfile` | ローカルでJekyllを動かすための依存指定 |

各Markdownの先頭にある `---` で囲まれた設定は、Jekyllのfront matterです。`layout`・`title`・`description`を設定しています。テーマにはGitHub Pages向けの[Cayman](https://github.com/pages-themes/cayman)を使用します。

サイト内リンクは `settings.html` など同じ階層の相対URLにしています。Markdownは公開時にHTMLへ変換されるため、GitHub上のMarkdownプレビューではなく、生成されたサイトでページ間を移動するためのリンクです。原稿をGitHubで読む場合はファイル一覧から各 `.md` を開いてください。

## ローカルで確認する

RubyとBundlerを使用できる環境で、リポジトリルートから次を実行します。依存gemの取得にはネットワーク接続が必要です。

```powershell
Set-Location docs
bundle install
bundle exec jekyll serve
```

起動ログに表示されたURL（通常は `http://127.0.0.1:4000`）で確認します。サイト生成だけを行う場合は `bundle exec jekyll build` を使います。テーマやCSSを含む確認方法は[GitHub公式のローカルテスト手順](https://docs.github.com/en/pages/setting-up-a-github-pages-site-with-jekyll/testing-your-github-pages-site-locally-with-jekyll)も参照してください。

リポジトリ名がURLの途中に入る構成をローカルで確認する場合は、例えば次のようにします。

```powershell
bundle exec jekyll serve --baseurl /BulkDistanceFade
```

この場合は `http://127.0.0.1:4000/BulkDistanceFade/` を開き、ナビゲーションとテーマの表示を確認します。`_site/` などの生成物は `.gitignore` に含めてあります。`Gemfile.lock` が生成された場合は、使用した依存バージョンの記録として扱ってください。

## 更新時の確認

- 設定や初期値を変えた場合は、`settings.md` とルート・パッケージ内のREADMEを合わせて更新します。ページはREADMEから自動同期されません。
- 検証した環境やテスト件数を更新するときは、`design/Implementation_Validation.md` の記録と実際の結果を照合します。
- ページを増減した場合は、各ページ冒頭の共通ナビゲーションと `index.md` の一覧を更新します。
- ページ間リンクは同じ階層の `.html` を指定します。ソースコードや設計書はPagesの `docs/` 外にあるため、公開サイトから届かない `../design/` などのリンクを作らず、リポジトリ内のパスとして案内しています。
- GitHub PagesでJekyllによる変換を使用するため、`.nojekyll` は追加しません。公開元をリポジトリルートへ変更すると、設計書や開発用ファイルも生成対象になるため、`/docs` を維持してください。


