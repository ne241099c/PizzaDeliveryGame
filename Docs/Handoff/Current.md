# 現在の引き継ぎ

更新：2026-10-23。**ユーザーの依頼でClaude CodeからCodexへ交代。** このファイル、[Workboard](Workboard.md)、[Codex向け再開手順](Codex_Resume.md)が現在の入口。古い段階の作業や全検証を最初から繰り返さない。

## 最初に読むこと

- **毎回の全検証は不要**。UIは変更箇所の表示・操作、計算・ゲーム処理は関連検証。全検証は大きな区切り、またはユーザーの明示依頼時だけ。引き継ぎ・再開・文書変更だけで長時間シミュレーションを回さない。
- 実装は既存との差分だけ。最新の追加依頼（[Dispatch_UX_Results.md](../Dispatch_UX_Results.md) の追加依頼(2)〜(4)）が、古い設計書・引用指示より優先する。配達指示はレーン方式、厨房に受け渡しはない、途中からは「その日の開店準備から」。
- 実装・Unity操作の担当はCodex。Claude Codeは終了。並行して同じUnity・同じファイルを編集しない。

## 現物の状態

- shop / ゲーム実装の最新 **ebe4c26**（この後に引き継ぎのdocsコミット）。mainは6bf94d6、shopは未統合、push なし。
- Unityは引き継ぎ時点でPlay中（Claudeが止めた後、Claude以外の操作で再開。ユーザーが遊んでいる可能性）。再開時に読み取りで確認する。
- 保護する未コミット変更：`.DS_Store`、`Packages/`の2つ、`.codex/`、`Assets/_Recovery`とmeta、Play中に変わる `Assets/Fonts/*Dynamic.asset`。
- 通常セーブはユーザーの2日目のプレイ。検証では保存抑制を必ず使う。

## 検証済みの記録（再実行の指示ではない）

- ebe4c26：単体740項目（M3/M4 170、M4 36、M5 67、reset 8、M6 135、M7 83、見た目132、店内51、画面58）と画面操作30段階が通過。
- シミュレーションは43e3cac以降未実行。3分営業・出費0.6倍・注文増・内勤の延長のバランスは未測定。

## 残作業と保留

[Codex_Resume.md](Codex_Resume.md) の「残作業・保留」。これらは情報であり、一括実装の指示ではない。

## 過去の情報

[Claudeへ交代した時のCurrent](Current_Before_Codex_20261023.md)、[その前のCurrent](Current_Before_Claude_20261023.md)、[Claude向け再開手順（当時）](Claude_Resume.md)。履歴中の担当や「毎段階で全検証」は現行指示ではない。
