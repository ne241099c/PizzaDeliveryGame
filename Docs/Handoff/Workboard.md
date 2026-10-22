# 共同作業ボード

更新: 2026-10-22。引き継ぎ準備時点。実行状況は各担当が追記する。

| 担当 | 作業 | 変更可能な範囲 | 状態 |
|---|---|---|---|
| Codex | Unity接続確認、V-4の実装・2解像度での撮影・全検証・ui→main統合 | UI表示コード、Editor配置/検証、シーン/Prefab/テーマ、最終画像・実装報告 | V-3完了。MCP 9085が接続拒否でV-4未着手 |
| Claude Code | V-4候補、既存UI、検証漏れ、資料の整合性の読み取りレビュー | `Docs/Handoff/Claude_Review.md` のみ | 参加用worktreeを準備。Claudeの実行・レビュー完了は未確認 |

## Claude Codeへ渡す最初の依頼

```text
AGENTS.md、CLAUDE.md、Docs/Handoff/Current.md、Docs/Handoff/Workboard.mdを読み、現状から参加してください。
担当はV-4の読み取りレビューです。ゲームやUIの実装、Unity操作、既存資料の変更、Git統合は行わないでください。
Docs/計画書_見た目.mdのV-4と、Docs/Handoff/V4_candidate.py.txt（未適用・未検証）を既存実装と照合してください。
とくにCanvasGroupとGraphic.enabledの関係、能力ラベルのサイズ、訓練UI、解像度変更、画面境界検証の不足を確認してください。
確実な不具合と提案を分け、ファイル名と行番号、再現条件、修正案、Unityで確認すべき点をDocs/Handoff/Claude_Review.mdへ記録してください。
候補スクリプトは実行しないでください。全検証の合格を推測しないでください。レビュー結果だけローカルコミットし、ハッシュを返してください。pushは禁止です。
```

## 作業の受け渡し

1. 担当が成果ファイル、コミット、確認したこと、未確認事項を返す。
2. Codexが元プロジェクトで `git show <hash>` を読み、担当範囲と除外対象を確認する。
3. 必要なコミットだけをcherry-pickする。Unityで検証が通る前に段階完了としない。
4. 次に実装を分担するときは、ここに具体的なファイルの所有者を追加してから作業する。

元プロジェクトに対して二人が同時にgit操作する運用にはしない。worktreeではLibrary/Tempを共有しない。Claudeの初回レビューではUnityを開かない。


## 作業場所

- Codex：`/Users/soma/Unity/PizzaDeliveryGame` / `ui`
- Claude Code：`/Users/soma/Unity/PizzaDeliveryGame/.worktrees/claude-review` / `claude/review-v4`
- `.worktrees/`はGit対象外。専用worktreeの`.git`は元リポジトリへの参照なので、フォルダ内のgit操作は専用ブランチに限る。
- Claude起動例：上の専用フォルダで `claude` を実行し、「Claude Codeへ渡す最初の依頼」を貼る。既に起動しているClaudeは元フォルダで編集せず、この専用フォルダから新しいセッションを開始する。
- 最初のレビューにMCP接続設定は不要。認証付き設定をworktreeへコピーしない。
