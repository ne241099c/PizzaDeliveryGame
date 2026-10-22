# 現在の引き継ぎ

更新：2026-10-23。**ユーザーの依頼でCodexからClaude Codeへ交代。** このファイルとWorkboardが現在の入口。古い段階の作業や全検証を最初から繰り返さない。

## 最初に読むこと

- **毎回の全検証は不要**。UIは変更箇所の表示・操作、計算・ゲーム処理は関連検証。全検証は大きな区切り、またはユーザーの明示依頼時だけ。引き継ぎ・再開・文書変更だけで長時間シミュレーションを回さない。
- 実装は既存との差分だけ。最新の追加依頼が古い設計書・引用指示より優先する。
- 実装・Unity操作担当はClaude Code。Codexは終了。並行して同じUnity・同じファイルを編集しない。
- [Claude向け再開手順](Claude_Resume.md)、[作業ボード](Workboard.md)、ルートの `AGENTS.md` と `CLAUDE.md` を読む。

## 現物の状態

- プロジェクト：`/Users/soma/Unity/PizzaDeliveryGame`。Unity 6000.2.10f1、`Assets/Scenes/SampleScene.unity`。
- 元フォルダは **shop**。ゲーム実装の最新は **43e3cac**（配達指示UX）。この引き継ぎ文書のコミットがその後に続く。HEADは `git log` を正とする。
- `main` は **6bf94d6**（V-4まで統合済み）。新しいshopの作業はmainへ未統合。pushしていない。
- **引き継ぎ時の読み取りではPlay中**、一時停止なし、コンパイル/更新なし。シーンisDirty=false、営業Open、AutomaticTick=true、RenderingSuspended=false。Codexは今回Playを止めたり営業を初期化したりしていない。進行する状態なので再開時に読み取り確認する。
- Codexによる検証・撮影・再コンパイル・バックグラウンド作業は実行していない。
- 残る未コミット変更は `.DS_Store`、`Packages/manifest.json`、`Packages/packages-lock.json`、`.codex/`、`Assets/_Recovery.meta`、`Assets/_Recovery/`。保護し、コミットに混ぜない。

## 最新の仕様・実装

- 厨房と配達指示の2画面。受け渡し専用タブを非表示とし、積み込みを配達指示へ統合。
- 手作りでは窯から取り出すと2秒で自動梱包。内勤は取り出しも自動。品質・焦げは維持。ラック満杯なら待ち、空けば再開。
- 配達指示：配達員の名前 → 注文の「積む」→「届け先へ」→「出発」。地図の道を直接選ぶ方法、おすすめ経路、個別配達との時間比較も残す。
- 注文ごとに調理/積載/配達の状態、目安までの秒数、遅れと受付猶予、配達中の到着予定を表示。キャンセル理由も8秒残す。
- 未出発注文は元の目安+20秒まで受付。元の目安時刻は書き換えない。出発済みは遅れても配達成功、既存の毎秒5点減点・下限0を使用。遅配にはチップ/品質ボーナスなし。閉店時の未出発キャンセルは維持。
- 詳細・変更ファイル・判断は [Dispatch_UX_Results.md](../Dispatch_UX_Results.md)。`ManualDeliveryRoute`、`RoadPathfinder`、`MapManager`、`MapDefinition`はこの変更で触っていない。

## 検証済みの記録（再実行の指示ではない）

- 43e3cacの全検証 **3733項目通過**。固定の単体/画面784、シミュレーション2949。通常方式の2回の結果と12 CSVが一致。
- Playの操作ハンドラー・道路の画面座標選択による20段階、設備表示2ケース通過。人間による面白さの評価とは別。
- [全検証記録](Dispatch_UX_Validation.json)、[操作記録](Shop_Manual_UI.json)、[設備記録](Shop_Station_UI.json)、[before/after 2解像度](../UI_Screens/dispatch-ux/README.md)。
- F/Gは30日完走、H（30秒指定、実際は物理下限約37.81秒）は2日で破産。既存経営係数の調整はしていない。

## 残作業と保留

- 店内UIの全S2/Dが完了したわけではない。残りは [Claude_Resume.md](Claude_Resume.md) と [復旧時の結果](../Shop_UI_StageResults.md) を参照。新しい追加指示を旧仕様へ巻き戻して埋めない。
- V-5、M4-9、C-5後のバランス調整、任意S2-6は未実施・保留。main統合や全残項目の一括実装を、この引き継ぎだけで新たに指示されたと解釈しない。
- 別worktreeの文言コミットe99263aはshopへ未統合。取り込む場合は現行ShopStringsとの重複を先に確認。

## 過去の情報

[交代前のCurrent全文](Current_Before_Claude_20261023.md)、[交代前のWorkboard全文](Workboard_Before_Claude_20261023.md)に以前の仕様・段階・ツール事情を保存した。履歴中の所有者、ブランチ、「毎段階で全検証」は現行指示ではない。
