# 共同作業ボード — 現在

更新：2026-10-23。ユーザーの「Codexに引き継ぐ」という依頼により交代。

| 担当 | 状態 | 作業場所・範囲 |
|---|---|---|
| Codex | **実装・Unity操作の担当**。[Codex_Resume](Codex_Resume.md)から再開 | 元プロジェクト / shop。既存UI・配達UXと関連するコード・検証・資料。範囲はユーザーの依頼に合わせる |
| Claude Code | 追加依頼(2)〜(4)を実装・コミット（3072c16、acdfba5、ebe4c26）。引き継ぎ資料を作成し、Unityの操作権を解放 | 引き継ぎ後は同じUnityや元フォルダを並行編集しない |

**Unity操作の所有者：Codex。** 起動依頼やメッセージの自動送信はしていない。ユーザーがCodexで再開する。

## 現在の状態

- shop / **ebe4c26**（続くdocsコミットが今回の引き継ぎ）。main 6bf94d6、未統合、pushなし。
- 引き継ぎ時のUnity：Claudeが止めたPlayがClaude以外の操作で再開しており、Play中。Claudeの実行中ジョブ・検証・撮影・プロセスはない。
- Play終了直後のEditモードでシーンがdirtyだった（原因不明・未保存）。コミット済みのシーンが正。
- 未コミットのPackages、.DS_Store、.codex、Assets/_Recoveryとmeta、動的フォントのasset 4つはコミットしない。

## 検証方針 — 最新のユーザー指示

**毎回/毎段階で全検証をしない。** UIは変更箇所の表示と操作、計算・ゲーム処理は関連する検証。全検証は大きな区切りか明示依頼時。

## 再開する入口

1. [Current](Current.md)、[Codex向け再開手順](Codex_Resume.md)、AGENTS.mdを読む。
2. gitの状態とUnityの状態を読み取り確認する。ユーザーのPlayを検証fixtureで初期化しない。
3. 最新の配達UX（レーン方式）を維持し、次の依頼の差分だけを実装する。
4. 確認は変更箇所に絞り、行った確認と未確認を記録してローカルコミット。pushしない。

最新結果：[配達UX](../Dispatch_UX_Results.md)（追加依頼(2)〜(4)）、[画面操作記録](Shop_Manual_UI.json)、画像 `Docs/UI_Screens/dispatch-lanes/`・`dispatch-lanes2/`。

過去：[Codexへ交代前のWorkboard](Workboard_Before_Codex_20261023.md)、[Claudeへ交代前のWorkboard](Workboard_Before_Claude_20261023.md)。
