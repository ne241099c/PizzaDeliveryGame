> **最新：目標と報酬の機能実装が完了。** [結果・操作・判断点](../Results/Rewards_Implementation.md)を参照。shop、関連226項目通過、通常セーブ/recordsは前後一致、新profileの生成なし。R-6は仮UI（正式モック未提供）、R-7測定の数値調整はユーザー判断待ち。旧プレイは借金なしエンドレスへ移行し、新規通常モードは20日。Unity編集モード、Codex所有、pushなし。以下は過去の引き継ぎ。

> 最新追補（2026-10-23）：Codexが速度1.2倍・窯FIFO・ピザ3種類・生成マップ・内勤方針を実装。最新はshopのgit logを参照。再開時は[Current](Current.md)と[実装/測定記録](../Results/Menu_Map_Implementation.md)を優先。G-4とJ/K/Lの数値調整は判断待ち。以下はClaudeから受け取った時点の履歴。

## 最新：失敗・作り直し、放置ピザ、初期5街区

Codexがshopで実装・関連245項目と実UI5項目を確認。詳細：[実装記録](../Results/Kitchen_Recovery_Implementation.md)。失敗品は保温棚の選択肢かゴミ箱へのドラッグで扱う。内勤は進行中の作業を中断せず、8秒放置されたピザを引き継ぐ。新規は5/8/10/12街区、旧生成セーブは3/5/8/12を維持。Unityは編集モード、push・main統合なし。最新コミット番号はgit logを正とする。

**セーブの未確定事項**：撮影前後は一致したが、全作業の開始時と現在の通常セーブは異なる。更新は11:28:14、Codex操作が止まっていた時間帯。ユーザーは操作したか覚えていない。原因を断定しない。元のコピーは発見できず復元済みではない。現在の1日目のファイルと最高記録をLibrary/ReviewBackups/20261023-123909に保全。最高記録は開始時と一致。過去の「2日目を保護」という記載を現在のファイル内容と取り違えない。


# Codexへの引き継ぎ — 2026-10-23（Claude Codeから）

ユーザーの依頼で、Claude Codeから実装・Unity操作をCodexへ戻す。Codexは実装・Unity操作の担当（レビュー専任ではない）。まず `AGENTS.md`、[Current](Current.md)、[Workboard](Workboard.md)、この文書を読む。

## 場所と状態（引き継ぎ時に確認した事実）

- `/Users/soma/Unity/PizzaDeliveryGame`、ブランチ **shop**。ゲーム実装の最新は **ebe4c26**。その後にこの引き継ぎのdocsコミットが続く。HEADは `git log` を正とする。
- mainは6bf94d6のまま。shopは未統合。**pushしていない。**
- Unity 6000.2.10f1、`Assets/Scenes/Game.unity`。
  - Claudeは最後にPlayを止めたが、その直後に（Claudeの操作ではなく）Playが再開された。ユーザーが遊んでいる可能性があるので、Codexは再開時に状態を読み取り、ユーザーのPlayを検証fixtureで初期化しない。
  - Play終了直後のEditモードで、シーンが `isDirty=true` になっていた（原因未特定。ClaudeはApplyや保存をしていない）。**ディスク上のシーン（コミット済み）が正**。理由の分からないdirtyをそのまま保存しない。
- 未コミット変更（コミット対象外として保護）：`.DS_Store`、`Packages/manifest.json`、`Packages/packages-lock.json`、`.codex/`、`Assets/_Recovery.meta`、`Assets/_Recovery/`。
  - これに加えて `Assets/Fonts/*Dynamic.asset` の4つ。Play中にTextMeshProが文字を足したことで変わる動的フォントで、作業の成果ではない。コミットしない（戻すならPlay外で `git checkout` してよい）。
- 通常セーブ `~/Library/Application Support/DefaultCompany/PizzaDeliveryGame/campaign.json` は**ユーザーのプレイ内容（2日目・11件配達・通算¥846）**。Claudeの検証では上書きしていない。`records.json`（最高記録）はまだ存在しない。

## Claudeが今回入れたもの（43e3cac以降、3コミット）

詳細・判断・確認結果は [Dispatch_UX_Results.md](../Results/Dispatch_UX_Results.md) の「追加依頼(2)〜(4)」。

1. **3072c16** 注文の進行表示、開店時の注文、ボタン中身の中央揃え、梱包0秒、保温棚のストック、目安スライダ。
2. **acdfba5** 指示盤を**レーン方式**に変更（設計書_店内シーン 改訂2、承認モック `Docs/UI_Mock/ShopDispatch.png`・`Batch.png`）。
   - 下に配達員ごとのレーン。伝票（一覧・ピン）をドラッグで入れ、スロット間ドラッグで順番を変え、一覧へ戻すと外れる。
   - 自動ルートは店→届け先→店の最短距離（混雑・土地勘を見ない素直な案、設計どおり）。
   - 伝票を持つと近いピンが光り、レーンに「まとめると/別々だと」の目安。
   - 注文一覧に焼き上がり目安（`CookingManager.ReadyIn`、表示専用）。
   - レーンに体力・運転・土地勘のバー。左上に厨房/配達指示のトグル。
   - 文字は枠に合わせて縮小（`UiKit.Label` の自動縮小）。
3. **ebe4c26** ユーザーの追加依頼(4)。
   - 営業180秒。日給・固定費を0.6倍（シーンとコードの初期値の両方）。
   - 注文の間隔10秒・開店時3件。内勤の工程を延長（標準18秒）。
   - 赤いルート線のどこでもドラッグでき、指している道に即時切り替え（道そのものを経由地にできる）。行き帰りの三角矢印。
   - Esc・遊び方・タイトル確認をモーダルに（`Shop/ShopModals.cs`。旧パネルはShopUI配下へ移して非表示の子として保持）。
   - 途中からは「**その日の開店準備から再開**」（ユーザーが選択）。タイトルに説明を表示。
   - 最高記録 `records.json`（保存抑制中は書かない）。
   - 厨房の受け渡しを削除。上のバー（時間・所持金・評判・目安スライダ）を配達画面にも表示。

ゲームの数値（時間・単価・間隔）を変えたので、それを固定値で持っていた検証（M3/M5/M6/M7/Shop）の期待値を新しい値に直した。条件を緩めたのではなく、同じ式の新しい値。

## 確認済み（再実行の指示ではない）

- ebe4c26時点の単体：M3/M4 170、M4 36、M5 67、reset 8、M6 135、M7 83、見た目 132、店内 51、画面 58。すべて通過。
- 画面操作 `Tools/Review/validate_shop_ui.py` は30段階通過（[記録](Shop_Manual_UI.json)）。
- **未実行：シミュレーション（M5/M6/M7の自動営業・30日比較）**。3分営業・出費0.6倍・注文増・内勤の延長で、収支のバランスは大きく変わっているが未測定。黒字になるか、破産しやすさは不明。数値の再調整はユーザー判断。

## 注意（今回わかったこと）

- **通常セーブを守る**：検証や撮影のスニペットは、全体を `using var s=save.SuppressSaving();` で包む。
  - 包んでも、スコープが終わった後のフレームで開店準備イベントや日の精算が起きると書き込まれうる。そのため最後に「保存抑制のままStartNewGame→タイトルへ」で片付ける（validate_shop_ui.pyのfinallyと同じ）。
  - 以前、撮影補助が通常セーブを上書きした事故がある（Dispatch_UX_Resultsに記録）。
- `execute_code` は、Playしていなくても実行されてしまう。fixtureの先頭で `if(!Application.isPlaying)` を確認する。Editモードで実行するとシーンのオブジェクトを書き換えうる。
- KitWrightの `simulate_mouse_drag` は、このエディタではゲームのInput Systemに届かなかった（地図のパンも動かない。Gameビューのフォーカスや入力設定を変えても同じ）。
  - 地図操作は `BusinessView.PointerDown/PointerMove/PointerUp`（実際のマウスが通る同じ処理）を座標で呼んで確認する。
  - `BusinessView.InputTrace` に、地図が受けた最後の押下が残る。
  - ルートのドラッグを人が実際のマウスで触った確認は、まだユーザーから結果をもらっていない。
- 旧UIの各画面（タイトルなど）は `UIThemeDriver` が毎フレーム装飾し直す。新しい部品は `ShopUiRoot`（Canvas/ShopUI）の下に作れば対象外になる。モーダルが旧パネルをShopUI配下へ移したのはこのため。
- Editorスクリプトを変えたら、Playを終了→再コンパイル→Play。`request_recompile` はPlay中は拒否される。
- 撮影は `Tools/Review/capture_shop_views.py`（既存）。Claudeは一時的な撮影補助をscratchで使っただけで、リポジトリには追加していない。

## 残作業・保留（新しい指示ではない）

- 収支バランスの測定と、必要なら調整（上記のとおり未実行）。
- 人によるルートドラッグ・レーン操作の使い心地の確認。
- タイトル画面の新デザイン（保留中。今回はタイトルの文言だけ変更）。
- 旧資料にある残り：大きな地図のチャンク/LOD/負荷測定、任意S2-6、V-5、M4-9、C-5の数値調整、shop→main統合。
- 別worktree `.worktrees/codex-text`（`codex/v4-text`、e99263a、UiStringsの文言）はshopへ未統合。[Codex_Implementation.md](Codex_Implementation.md) は当時の記録。

## コードの入口

- `Assets/Scripts/UI/Screens/Business/BusinessView.cs`：2画面、トグル、上のバー、伝票、通知。
- `BusinessView.Dispatch.cs`：レーン、ルート（区間・経由地・自動ルート・ドラッグ）、ピン、矢印、地図入力。
- `BusinessView.Orders.cs`：注文一覧と焼き上がり目安。
- `BusinessView.Kitchen.cs`：厨房と保温棚。
- `ShopModals.cs`／`TitleMenuUI.cs`：モーダル。
- `CampaignSaveManager.cs`：セーブと最高記録。`HandoverManager.cs`：バッグのモデル（`Move`で順番入れ替え）。
- `ManualDeliveryRoute`・`RoadPathfinder`・`MapDefinition` は今回も変更していない。
