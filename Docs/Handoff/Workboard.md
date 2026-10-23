> 完了：S2-7の内勤方針・代役調理の5項目通過、J/K/Lを30日上限で測定。J/Kは初日、Lは6日目に破産。正常保存の前後一致を確認。数値調整は判断待ち。

> 完了：Codexが速度1.2倍・窯FIFO・ピザ3種類・生成マップG-0〜G-5を実装。関連検証、1000シード、100街3日が通過。通常資金の段階別測定は初日破産で数値判断待ち。詳細：[実装記録](../Results/Menu_Map_Implementation.md)。S2-7のJ/K/Lは追補で実装・測定済み。shopで段階別ローカルコミット、pushなし。

> 完了：Codexが追加4件を実装。関連441項目通過、8人分のレーンと新ルートをPlay表示確認。通常セーブ・最高記録は前後一致。Unityは編集モード。詳細：[修正結果](../Results/Staff_Route_Resume_Results.md)。shopでローカルコミット（番号はgit log）、pushなし。

# 共同作業ボード — 現在

> **2026-10-23 12:50 交代：ユーザーの依頼でCodexからClaude Codeへ実装・Unity操作を引き渡し。** **Unity操作の所有者：Claude Code**（Codexは同じUnityと元フォルダを操作しない）。
> Claude完了（shop、pushなし）：a9bc3e6 D-5aタイトル（プレハブ・GameInfo・セーブ置き場の固定）、d7f17c1 整理（Docs/Archive、古い*SceneSetup削除、TMPサンプル削除、.gitignore）、d28f01e asmdef＋Test Runner、72aee10 精算・閉店のプレハブ化、a86dda4 開店準備のプレハブ化。Unityは編集モード。詳細と残作業は[Current](Current.md)の最新節。


> Codex完了：S2-3/S2-4の失敗・作り直し、放置ピザの内勤引き継ぎ、初期5街区。関連245項目＋UI5項目通過。shop / Unityは編集モード。セーブの全期間差異は原因未確定、復元済みとはしない。現行ファイルをLibrary/ReviewBackups/20261023-123909に保全。詳細は [実装記録](../Results/Kitchen_Recovery_Implementation.md)。

更新：2026-10-23。ユーザーの「Codexに引き継ぐ」という依頼により交代。

| 担当 | 状態 | 作業場所・範囲 |
|---|---|---|
| Codex | **実装・Unity操作の担当**。[Codex_Resume](Codex_Resume.md)から再開 | 元プロジェクト / shop。既存UI・配達UXと関連するコード・検証・資料。範囲はユーザーの依頼に合わせる |
| Claude Code | 追加依頼(2)〜(4)を実装・コミット（3072c16、acdfba5、ebe4c26）。引き継ぎ資料を作成し、Unityの操作権を解放 | 引き継ぎ後は同じUnityや元フォルダを並行編集しない |

（履歴）Codexが所有していた時の記録。現在の所有者は上の交代記録を参照。

## Claudeからの引き継ぎ時の状態（履歴）

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

最新結果：[配達UX](../Results/Dispatch_UX_Results.md)（追加依頼(2)〜(4)）、[画面操作記録](Shop_Manual_UI.json)、画像 `Docs/UI_Screens/dispatch-lanes/`・`dispatch-lanes2/`。

過去：[Codexへ交代前のWorkboard](Workboard_Before_Codex_20261023.md)、[Claudeへ交代前のWorkboard](Workboard_Before_Claude_20261023.md)。
