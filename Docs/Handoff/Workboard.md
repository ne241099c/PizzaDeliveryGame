> **完了（2026-10-24）：Claude Codeが道路網v8（家は道の途中・交差点増）、道の最低の太さ、道のクリックで経路変更、押している間の早送り、配達指示の説明削除を実装。** Test Runner 26件通過、セーブ前後一致。詳細：[記録](../Results/City_Network_v8.md)。Unity操作担当：Claude Code、Unityは編集モード。

> **完了（2026-10-24）：Codexが利用制限で停止、Claude Codeが引き継いで道路網v7・配達画面・家の増加を仕上げ。** 家は地区あたり7軒（最大100軒）、配達先の家を色分け、検証の高速化、古い検証の期待値更新。Test Runner 25件通過、セーブ前後一致。詳細：[記録](../Results/City_Network_v7.md)。Unity操作担当：Claude Code（交代時はここを更新）、Unityは編集モード。

> **現在：Codexが街区骨格を使わない道路網v7と配達画面UXを実装。** shop / 8bd8be4。生成は専用street-network worktree、配達画面はdispatch-ready worktree。統合・Unity・対象検証・描画対応はCodex本体。マップ拡大／一方通行削減／配達画面で低速／常設案内削除／出発可能通知を今回の範囲とする。

> **完了（2026-10-24）：Codexが不規則な街路生成v6を実装。** 関連179項目通過、異なるseedの地図2枚を確認。通常セーブ・最高記録はPlay前とバイト一致。道路を共有する密集街区、寸法・交差点・住宅路の向き・枝道・行き止まりが変化。街の大枠は街区ベース。詳細：[記録・画像](../Results/City_Organic.md)。shop、pushなし。Unity操作担当Codex、編集モードへ戻す。

> **現在：Codexが現実の街に近い不規則な道路網への改善を担当。** shop / 90690c5、開始時Unity編集モード。生成処理は専用worktree、描画・保存互換・対象検証・Unity操作は本体。四方向への配達先と帰還可能性を維持。既存フォント・Packagesは対象外。

> **完了：Codexが店舗四方の地図v5・調理前予約・道順編集中0.1倍・最大3件・成長表示と経験倍率・設備Lv10・地図外通知を実装。** shop、ローカルのみ、pushなし。関連264項目と厨房スクロール追加4条件が通過。通常セーブ／最高記録は開始時とバイト一致。Unity編集モードへ復帰。画面2枚と判断事項は [実装記録](../Results/Shop_Planning_Reservations.md)。既存フォント・Packages変更は保護。次のUnity操作担当はCodex（交代時はここを更新）。

> **現在：ユーザーのClaude完了報告と追加依頼によりCodexが実装・Unity操作を再開。** shop / c4a4bd3。今回の範囲は四方への配達先、経路計画中の時間確保、焼成前予約、3件持ち、成長表示・速度、設備上限、通知。マップと成長・設備は独立worktree、統合とUnity操作はCodex本体のみ。開始時Unity編集モード。既存フォント・Packages変更は保護。

> **現在（2026-10-23 夜）：Codexが利用制限で停止したため、ユーザーの依頼でClaude Codeが引き継ぎ。Unity操作の所有者：Claude Code。** Codexの未コミット分を確認・検証し、小さな修正2件を加えてコミット済み（詳細：[実装・確認記録](../Results/Shop_UX_Structure_Improvements.md)の最終節）。Unityは編集モード。Codexは同じUnityを操作しない。

> （直前）**現在：Codexが追加UX改善・家の増加の実装とUnity操作を担当。** 開始時 shop / b647cfe、Unityは編集モード・コンパイルなし。Claudeの完了コミット後、ユーザーの最新依頼で再開。独立したソース作業は /private/tmp の専用worktreeで分担し、Unity操作・統合はCodex本体のみ。

> 完了：S2-7の内勤方針・代役調理の5項目通過、J/K/Lを30日上限で測定。J/Kは初日、Lは6日目に破産。正常保存の前後一致を確認。数値調整は判断待ち。

> 完了：Codexが速度1.2倍・窯FIFO・ピザ3種類・生成マップG-0〜G-5を実装。関連検証、1000シード、100街3日が通過。通常資金の段階別測定は初日破産で数値判断待ち。詳細：[実装記録](../Results/Menu_Map_Implementation.md)。S2-7のJ/K/Lは追補で実装・測定済み。shopで段階別ローカルコミット、pushなし。

> 完了：Codexが追加4件を実装。関連441項目通過、8人分のレーンと新ルートをPlay表示確認。通常セーブ・最高記録は前後一致。Unityは編集モード。詳細：[修正結果](../Results/Staff_Route_Resume_Results.md)。shopでローカルコミット（番号はgit log）、pushなし。

# 共同作業ボード — 現在

> **2026-10-23 12:50 交代：ユーザーの依頼でCodexからClaude Codeへ実装・Unity操作を引き渡し。** **Unity操作の所有者：Claude Code**（Codexは同じUnityと元フォルダを操作しない）。
> Claude完了（shop、pushなし）：a9bc3e6 D-5aタイトル（プレハブ・GameInfo・セーブ置き場の固定）、d7f17c1 整理（Docs/Archive、古い*SceneSetup削除、TMPサンプル削除、.gitignore）、d28f01e asmdef＋Test Runner、72aee10 精算・閉店のプレハブ化、a86dda4 開店準備のプレハブ化。Unityは編集モード。詳細と残作業は[Current](Current.md)の最新節。
> 第2弾（同日午後）：旧UI撤去とTitle/Gameのシーン分割、フォルダ整理、カード・店内のプレハブ化、セーブの保護（SessionSavingLocked）、起動時の街の保存バグ修正、長い行の整形、名前検索の撤去。詳細は[Current](Current.md)の「構造の作り直し（第2弾）」。


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
