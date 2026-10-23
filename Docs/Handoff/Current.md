> **完了（2026-10-24）：Codexが配達画面のズーム・表示位置の保持、配達員と内勤の即時有料訓練を実装。** 関連88項目通過、通常セーブ・最高記録は前後バイト一致。shop、pushなし、Unity編集モード。Unity操作担当Codex。詳細：[実装記録](../Results/Zoom_Instant_Training.md)。添付目標報酬は受領済み、全体の実装範囲は未回答。

> **完了（2026-10-24）：Codexが不規則な街路生成v6を実装。** 関連179項目通過、異なるseedの地図2枚を確認。通常セーブ・最高記録はPlay前とバイト一致。道路を共有する密集街区、寸法・交差点・住宅路の向き・枝道・行き止まりが変化。街の大枠は街区ベース。詳細：[記録・画像](../Results/City_Organic.md)。shop、pushなし。Unity操作担当Codex、編集モードへ戻す。

> **完了：Codexが店舗四方の地図v5・調理前予約・道順編集中0.1倍・最大3件・成長表示と経験倍率・設備Lv10・地図外通知を実装。** shop、ローカルのみ、pushなし。関連264項目と厨房スクロール追加4条件が通過。通常セーブ／最高記録は開始時とバイト一致。Unity編集モードへ復帰。画面2枚と判断事項は [実装記録](../Results/Shop_Planning_Reservations.md)。既存フォント・Packages変更は保護。次のUnity操作担当はCodex（交代時はここを更新）。

# 最新：配達・厨房の操作と構成の改善（Codex、2026-10-23）

ユーザーの追加依頼でCodexが実装・Unity操作を担当。shop、pushなし。作業開始はb647cfe。詳細：[実装・確認記録](../Results/Shop_UX_Structure_Improvements.md)。

- スタッフ・レーンのスクロール、帰還ゲージ、1.6の速度倍率、状態別ピン、注文番号、裏道の形とクリック選択、荷物クリック解除、順番入替、注文カード、窯待ちピザ、廃棄0、イカ縮小、配達員拡大を実装。
- 家は開始22軒。既存v3の街も、元の家・道を維持できる場合に追加する。通常セーブを直接書き換えず、保護したPlayの「続きから」で4日目・22軒を確認した。
- GameManagerは4つの子グループに整理（26管理Script・185参照、欠落0）。固定UIのPrefabは必要箇所だけを更新。再生成にはバックアップと上書き確認。
- 読めないセーブの上書き防止、壊れた最高記録、作り直し時間換算も修正。関連193項目通過。長時間シミュレーション・全検証は再実行していない。
- Codexが利用制限で停止したため、Claude Codeが引き継いで最終確認・コミット（配達中レーンのゲージ配置とタイトルのセーブ破損メッセージを修正。Test Runner 25件中24件通過、失敗したTitleは修正後に関連5件と再実行して通過。セーブはバイト一致）。
- ManualDeliveryRoute/RoadPathfinderは未変更。Packages・動的フォント等の既存変更は含めない。

以下は過去の記録。担当と状態はこの最新節およびWorkboardの先頭を正とする。

# 現在の引き継ぎ

## 最新：D-5aタイトルと構造の整理（Claude Code、2026-10-23）

担当：**Claude Code（Unity操作の所有者）**。shop、pushなし。第1弾 a9bc3e6 → d7f17c1 → d28f01e → 72aee10 → a86dda4、第2弾は下の節。

- **タイトル（D-5a）**：`Assets/Prefabs/UI/TitleView.prefab`（`TitleView`、生成 `TitleViewBuilder`）。絵は `Tools/Art/title_art.py` → `Assets/Art/Title/`。ゲーム名は `Assets/Scripts/Core/GameInfo.cs` だけで定義し、**Tools > Game Info > Sync** でproductName・README・クレジットへ反映。チュートリアルは未実装なので「準備中」。
- **セーブ置き場**：productNameを変えるとUnityのデータフォルダ名が変わるため、セーブは固定の `GameInfo.SaveFolder`（PizzaDeliveryGame）に置く。**SaveFolderは変えない**。通常セーブ・最高記録は作業の前後でバイト一致。
- **プレハブ化**：タイトル・開店準備（枠）・精算（枠）・閉店は `Assets/Prefabs/UI/`。生成は `ScreenPrefabs`（Tools > Screens）。作り直すとプレハブの手修正は消える。データで変わるカード・レシート行・スタッフ行は実行時に作る。
- **asmdef**：`PizzaDelivery`（実行時）、`PizzaDelivery.Editor`、`PizzaDelivery.Tests`。execute_code・Tools/Reviewはそのまま動く。
- **Test Runner**：Tools > Pizza Delivery > Run Tests（結果は `Library/TestResults/last.txt`）。編集モード5件＋Play検証9件（タイトル、M3〜M6、画面、見た目、M7、店内、プレハブ画面、起動2件）。シミュレーションは含まない。各テストはTitleシーンでPlayを始め、保存を止めてからGameシーンを読み込む。
- **整理**：古い資料は `Docs/Archive/`。古い `*SceneSetup` は削除（現在のシーンを壊すため）。TMPのサンプル削除。`.DS_Store`・`Assets/_Recovery`・`.codex/` は .gitignore。
- **直した既存の不整合**：M6の配達時間の期待値（速度1.2倍）、Visualの街区名（District_）、validate_shop_ui.pyの保温棚の階層、UIThemeDriverの毎フレーム例外（CanvasRendererのないボタン）。
- **シーンのdirtyの正体**：非表示の旧UIにあった `ContentSizeFitter`。旧UIの撤去でなくなった。

### 構造の作り直し（第2弾、2026-10-23 午後）

コミット 0192ccc → a277d2d → bf9567b → （このあとの整形・名前検索の撤去）。

- **シーンは2つ**：`Assets/Scenes/Title.unity`（タイトル）と `Game.unity`（旧SampleScene）。ビルドはTitle→Game。`GameScenes` が選んだモード（続きから／最初から）を渡す。Gameを直接再生すると、使えるセーブがあれば続きから。**タイトルへ戻る＝Titleシーンの読み込み**。
- **旧UIを撤去**：旧画面・テーマの上書き処理・一覧用プレハブと16スクリプトを削除。`OrderListUI`→`OrderBook`、`DeliveryPersonSelector`→`CourierRoster`、`DeliveryPlannerUI`→`DeliveryPlanner` はデータと処理だけ。`TitleMenuUI`→`GameMenu`（営業中のEscメニュー）。
- **プレハブ**：`Assets/Prefabs/UI/`（Title、Hub、Business、Settlement、GameOver）と `UI/Cards/`（配達員・求人・内勤・設備・仲間を探す・経営）。生成は Tools > Screens（`ScreenPrefabs`）、店内は `BusinessView.BuildLayout`（エディタで一度作り、動作は実行時に `Bind`）。作り直すと手修正は消える。
- **フォルダ**：README.mdの「フォルダ」の表のとおり（Scripts・Editor・Docsを分野別に）。
- **セーブの保護**：`CampaignSaveManager.SessionSavingLocked` はPlay中ずっと、シーンを読み直しても保存を止める。テストと `Tools/Review/session.py` はGameシーンが始まる前にこれを立てる。検証の後片付けはシーンを切り替えない。
- **直したバグ**：シーン分割の後、Gameシーンが地図の準備前にゲームを始め、新しいゲームの街が空（blocks=0）で保存されていた。次の起動でそのセーブが拒否され、また新しいゲームに上書きされる。地図の準備後に始めるよう修正し、テスト（LaunchContinue/LaunchNewGame）を追加。
- **ユーザーのセーブ**：14:22にユーザーが遊んだ2日目のセーブを `Library/ReviewBackups/20261023-142254-user` に保全。15:51と15:55に新しいゲームで上書きされ（15:55は上記のバグによるもの。15:51は、同じ頃に検証の後片付けがTitleシーンへ切り替えていたが、どの操作で新しいゲームが始まったかは特定できていない）、どちらも壊れた街の状態だったため、**2日目のセーブを戻した**。上書きされたものは `20261023-155132-overwritten` に保存。
- **コードの書き方**：`Tools/Format/split_long_lines.py` で、1行に詰めた複数の文を1行1文に分けた（空白と改行だけを変え、コードの中身が同じであることをファイルごとに確認してから書き込む）。150字超は957→473行、200字超は405→163行（残りは1つの長い式）。実行時コードの名前の文字列での探索（`Find("...")`）は0件に（部品を参照で持つ `UiBadge`・`UiAvatar`・`UiCard` と、生成時に保持する変数へ）。
- **Codexの文書**：16:31に `Docs/Design/構成の改善点.md`（Codex、文書のみ）が追加されている。Claudeの作業中に書かれたもので、未コミットのまま残している。内容は今回の方針と同じ。

### 残り・保留

- 長い1つの式（200字超163行）は機械的に折り返すと読みにくくなるため、触るときに直す。
- 管理クラスの探索（`FindFirstObjectByType`）はUnityの一般的な書き方として残した。シーン参照に置き換えるなら次の候補。
- 旧からの保留（収支バランス、チュートリアルS2-T、大きな地図の負荷など）は下の節のとおり。

## 最新：失敗・作り直し、放置ピザ、初期5街区

Codexがshopで実装・関連245項目と実UI5項目を確認。詳細：[実装記録](../Results/Kitchen_Recovery_Implementation.md)。失敗品は保温棚の選択肢かゴミ箱へのドラッグで扱う。内勤は進行中の作業を中断せず、8秒放置されたピザを引き継ぐ。新規は5/8/10/12街区、旧生成セーブは3/5/8/12を維持。Unityは編集モード、push・main統合なし。最新コミット番号はgit logを正とする。

**セーブの未確定事項**：撮影前後は一致したが、全作業の開始時と現在の通常セーブは異なる。更新は11:28:14、Codex操作が止まっていた時間帯。ユーザーは操作したか覚えていない。原因を断定しない。元のコピーは発見できず復元済みではない。現在の1日目のファイルと最高記録をLibrary/ReviewBackups/20261023-123909に保全。最高記録は開始時と一致。過去の「2日目を保護」という記載を現在のファイル内容と取り違えない。


更新：2026-10-23。**ユーザーの依頼でClaude CodeからCodexへ交代。** このファイル、[Workboard](Workboard.md)、[Codex向け再開手順](Codex_Resume.md)が現在の入口。古い段階の作業や全検証を最初から繰り返さない。

## 最初に読むこと

- **毎回の全検証は不要**。UIは変更箇所の表示・操作、計算・ゲーム処理は関連検証。全検証は大きな区切り、またはユーザーの明示依頼時だけ。引き継ぎ・再開・文書変更だけで長時間シミュレーションを回さない。
- 実装は既存との差分だけ。最新の追加依頼（[Dispatch_UX_Results.md](../Results/Dispatch_UX_Results.md) の追加依頼(2)〜(4)）が、古い設計書・引用指示より優先する。配達指示はレーン方式、厨房に受け渡しはない、途中からは「その日の開店準備から」。
- 実装・Unity操作の担当はCodex。Claude Codeは終了。並行して同じUnity・同じファイルを編集しない。

## 最新：ピザ・窯キュー・生成マップ

Codexがshopで配達速度1.2倍、窯の共通FIFOキュー、S2-6ピザ3種類、G-0〜G-5の生成マップを実装。最終コミット番号はgit logを正とする。push・main統合なし。

[実装記録・判断・画像・検証](../Results/Menu_Map_Implementation.md)を先に読む。1,000シードの品質検査と100街×3日の実配達確認は通過。通常資金での4段階の30日上限測定は全て初日破産し、数値調整はしていない。S2-7も追補で実装・測定済み。J/Kは初日、Lは6日目に破産（30日上限）。数値調整は判断待ち。通常セーブ・最高記録のバイト一致を確認。新しい街は新規ゲームに適用し、旧セーブは元の地図を維持する。

Unity操作担当はCodex。通常セーブは既存の2日目を保護。資料にある毎段階の全検証は再実行せず、関連検証のみ行う。

## 最新の追加依頼：完了

Codexが従業員人数制限の撤廃、赤・青の行き帰り表示、タイトルの続ける修正、閉店後8倍速を実装。関連441項目と8人分のレーン・ルート表示を確認。通常セーブ・最高記録は前後一致。詳細：[修正結果](../Results/Staff_Route_Resume_Results.md)。Unityは編集モード。shopでローカルコミット、push・main統合なし。コミット番号はgit logを正とする。

## 引き継ぎ時の状態（履歴）

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
