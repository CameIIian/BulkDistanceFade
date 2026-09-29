---
layout: default
title: "公開時の安全チェック"
---

# 公開時の安全チェック

## 自動検査

リポジトリルートでPowerShell 7.4以上を使用します。

```powershell
./scripts/Test-PublicationSafety.ps1 -SourceOnly
./scripts/Test-PublicationSafety.ps1
./scripts/Test-VerificationSafety.ps1
```

最初のコマンドは公開対象ソース、2つ目はソースと`artifacts/`の配布アーカイブ、3つ目は検証用ファイル操作の境界を検査します。ZIPとunitypackageは展開せずに読み取ります。検出時はファイルパスとルール名だけを出し、検出値をログに表示しません。

検出対象は秘密鍵、代表的なGitHub／AWS／Slack／APIキー形式、認証情報入りURL、リテラルの認証情報代入、個人のホームディレクトリパスです。公開対象の環境設定・鍵ファイル、リンク／ジャンクションも拒否します。Git管理中の場合は追跡済みの検証環境・成果物・秘密ファイルも検査します。

各配布アーカイブは許可リストにあるRuntime、Editor、README、CHANGELOG、LICENSEと`.meta`に限定します。VPM ZIPだけが`package.json`とその`.meta`を含みます。Tests、SDK、NDMF、Shader、ライセンスキャッシュ、Git、ログ、検証補助コードは同梱しません。現在版はソースとの内容一致も検査します（公開URL等を追加するVPM manifestは別扱い）。

## 公開範囲

- `.verification~/`：Unity依存コピー、ライセンス関連ログ、検証データ、改名前バックアップ、旧成果物。公開しません。
- `artifacts/`：Gitには追加せず、確認済みファイルだけGitHub Releaseへ添付します。
- `.docx`：元のデザイン入力。文書プロパティ等を含み得るため公開対象から除外し、レビュー済みMarkdownを使用します。
- `.env*`、鍵、エディタ／エージェント設定、Unityキャッシュ：`.gitignore`で除外します。
- `docs/`：GitHub Pagesで公開します。`docs/index.json`には公開URL・連絡先メールが入ります。

VPM生成処理は認証トークンを要求せず、ネットワーク通信・アップロードも行いません。公開用メールアドレスは秘密ではなく、ZIPと一覧に含まれます。個人メールを出したくない場合は、受信可能な公開連絡先や公開を許容するGitHub noreplyアドレスを指定してください。

## 限界と公開前の確認

パターン検査では任意形式の秘密値や個人情報を完全には判別できません。Git履歴、GitHub設定、公開後のホスティング内容は別の確認対象です。`.gitignore`は追跡済みファイルや過去のcommitから情報を消しません。

公開前に`git status --short`と`git diff --cached --stat`で追加対象を確認し、`Test-PublicationSafety.ps1`を実行してください。既存履歴を公開する場合は履歴も確認してください。秘密がcommitされていた場合は、その秘密の失効・再発行と履歴からの除去が必要です。

公開するのは`artifacts/<version>/`内の指定ファイルだけです。作業フォルダー全体のZIP、Unityログ、バックアップをReleaseへ添付しないでください。SHA256は配布内容の照合用であり、作者の署名や安全性の保証ではありません。
