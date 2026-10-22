# PizzaDeliveryGame の共同開発

Codex / Claude Code 共通。ユーザーの最新の指示が優先する。

## 開始時

1. `Docs/Handoff/Current.md` と `Docs/Handoff/Workboard.md` を読む。
2. `git status --short`、現在のブランチ、HEADを確認する。資料の記録より実際の状態を優先し、差があれば記録する。
3. 担当と変更可能ファイルをWorkboardで確認してから着手する。未割り当てのコード作業は、まず読み取りレビューと提案にする。

## 分担と統合

- Codexは当面、元のプロジェクトのUnity Editor、シーン/Prefab/アセット編集、Play、全検証、撮影、統合を担当する。
- Claude Codeは専用Git worktreeと専用ブランチで作業する。元フォルダでcheckout/reset/mergeしない。同じUnity Editorを同時操作しない。
- 同じファイルを同時編集しない。シーン/Prefab、UITheme、共通表示ドライバ、検証ファイルは特に担当を明確にする。
- Claude Codeの初回担当は読み取りレビュー。成果は `Docs/Handoff/Claude_Review.md` に記載する。実装担当を受けた場合は指定ファイルのみ変更し、ローカルコミットのハッシュと検証状況を渡す。
- 統合担当が差分を読み、必要なコミットをcherry-pickし、元のUnityで全検証する。worktree上のソースレビューやPython構文確認を、Unity検証成功とは呼ばない。
- 作業の終わりにWorkboardへ、変更/検証/コミット/未完了/次の担当への注意を記録する。レビューだけなら結果ファイルのローカルコミットとハッシュを返す。

## 継続して守るユーザー指示

- pushしない。`Packages/`変更、`.DS_Store`、`.codex/`、`Assets/_Recovery`とそのmetaをコミットに含めない。既存の変更を消さない。`git add .` / `git add -A`は使わず、ファイルを明示する。
- 既存実装を調べて差分だけを変更する。現在のUI段階ではゲームのロジックとMapDefinitionを変更しない。ManualDeliveryRoute/RoadPathfinderは必要がない限り変更しない。
- 画面の表示/非表示はCanvasGroup。既存部品の再利用を優先。Canvas内の並びへの依存を先に調べる。色/文字サイズ/余白はUITheme、文言はUiStringsへ集める。
- `.unity` / `.prefab` / `.asset`はUnity Editor APIで編集する。シーンを外部テキスト置換しない。Unityのfake-nullには `??` ではなく明示的null判定を使う。
- 各段階は全検証が通った後だけ段階名（例 `V-4:`）を先頭にしてローカルコミットする。検証失敗時は次段階へ進まず、原因を報告する。
- V-5は対象外。M4-9の数値調整とM7 C-5後のバランス数値変更はユーザー判断待ち。未達を隠すために数値や検証条件を緩めない。
- 判断した仕様の曖昧さと限界を報告する。説明は日本語で、学習中のユーザーが操作の目的を理解できるようにする。
- 秘密情報・MCPの認証付きURL・設定ファイル全文を、資料/ログ/コミットへ転記しない。サーバーや承認機構を勝手に変更しない。
