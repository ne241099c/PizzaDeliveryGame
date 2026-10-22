# 共同作業ボード — 現在

更新：2026-10-23。ユーザーの「Claudeに引き継ぐ」という依頼により交代。

| 担当 | 状態 | 作業場所・範囲 |
|---|---|---|
| Claude Code | 2026-10-23 追加依頼(2)を実装・コミット済み（配達指示の進行表示、開店時の注文、ボタン中央、梱包0秒、棚のストック、配達員へのドロップ、目安スライダ）。単体787・画面操作24段階通過、シミュレーション未実行。通常セーブを誤って上書き（Dispatch_UX_Results参照） | 元プロジェクト / shop。既存UI・配達UXと関連するコード・検証・資料。変更範囲はユーザーの依頼に合わせる |
| Codex | 実装・検証・撮影終了。引き継ぎ資料を作成し、Unityの操作権を解放 | 引き継ぎ終了後は同じUnityや元フォルダを並行編集しない |

**Unity操作の所有者：Claude Code。** 起動依頼やメッセージの自動送信はしていない。ユーザーがClaude Codeで再開する。

## 現在の状態

- ゲーム実装の最新：shop / **43e3cac**。続くdocsコミットは今回の引き継ぎ。
- main：6bf94d6。shopは未統合、pushなし。
- 引き継ぎ確認時はUnityがPlay中、営業Open、AutomaticTick=true。コンパイル/更新なし、シーン保存済み、RenderingSuspended=false。Playの進行は止めていない。再開時の実際の状態を優先する。
- Codexが動かしている検証・撮影・プロセスはない。
- 未コミットのPackages、.DS_Store、.codex、Assets/_Recoveryとmetaは元からの変更。削除・リセット・コミットしない。

## 検証方針 — 最新のユーザー指示

**毎回/毎段階で全検証をしない。** UIは変更箇所の表示と操作、計算・ゲーム処理は関連する検証。全検証は大きな区切りか明示依頼時。引き継ぎだけで全3733項目を再実行しない。旧資料の全検証必須よりこの方針を優先する。

## 再開する入口

1. [Current](Current.md)、[Claude向け再開手順](Claude_Resume.md)、AGENTS.mdとCLAUDE.mdを読む。
2. gitの状態とUnityの状態を読み取り確認する。Play中の営業を検証fixtureで不用意に初期化しない。
3. 最新の配達UXを維持し、既存実装を調べて次の依頼の差分だけを実装する。旧受け渡しの手動梱包・目安即キャンセルへ戻さない。
4. 確認は変更箇所に絞り、今回行った確認と未確認を記録してローカルコミット。pushしない。

最新結果：[配達UX](../Dispatch_UX_Results.md)、[全検証記録](Dispatch_UX_Validation.json)、[画像](../UI_Screens/dispatch-ux/README.md)。残作業・保留・別worktreeはClaude_Resumeに記載。

過去の分担や詳細は[交代前のWorkboard全文](Workboard_Before_Claude_20261023.md)へ保存した。古いV-4分担やCodex所有の宣言は履歴。
