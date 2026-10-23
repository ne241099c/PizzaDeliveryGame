# Claude Codeへの引き継ぎ — 2026-10-23

## 今回のユーザーの意図

Claudeの利用制限中、Codexが店内UIを復旧し、追加依頼の配達操作改善まで実装した。ユーザーがClaudeへ戻して開発を続けるための引き継ぎ。Claudeはレビュー専任ではなく実装・Unity操作の担当。

**直近の重要な変更：ユーザーは検証に長時間かかることを問題にして、「毎回毎回やらなくていい」と指示した。** UI変更は表示・操作、計算/ゲーム処理の変更は関連検証に絞る。全検証は大きな区切りか明示依頼時。この新方針が以前の「毎段階で全検証」に優先する。引き継いだだけで全検証を開始しない。

## 再開時の場所と状態

- `/Users/soma/Unity/PizzaDeliveryGame` の **shop**。最新のゲーム実装は `43e3cac S2-5: 配達指示の注文操作と自動梱包・遅配猶予を実装`。その後に今回のdocsコミットが続くので実際のHEADも確認。
- その前は `31382d8 S2-1: 店内UIの途中実装を接続し表示と回帰検証を復旧`、Claudeの厨房処理は `57c7dba`、設計/承認モックは `52dc71b`。
- mainは6bf94d6。m4/m5/m6/screens/m7/uiの旧作業はV-4までmain統合済み。新しいshopはmainへ統合していない。pushしていない。
- Unity 6000.2.10f1。`Assets/Scenes/Game.unity`。引き継ぎ時は**Play中**、営業Open、AutomaticTick=true、一時停止/コンパイル/更新なし、isDirty=false、RenderingSuspended=false。CodexはPlayの停止やfixture初期化をしていない。状態は進行するので再確認する。
- Codexの実行中ジョブはない。ここからUnity操作を引き継いでよい。同じEditorを二人で操作しない。

残る未コミット変更（保護・コミット対象外）：

```text
 M .DS_Store
 M Packages/manifest.json
 M Packages/packages-lock.json
?? .codex/
?? Assets/_Recovery.meta
?? Assets/_Recovery/
```

## すでに動いているもの

1. 厨房・配達指示の2画面。専用受け渡しタブは非表示。旧ShopZone.Handoverの呼び出しはDispatchへ転送する互換処理。
2. プレイヤーは窯から取り出した後、既存packSeconds（初期2秒）で自動梱包。手動で箱へ置く/閉じる操作は不要。ラック6個の予約と満杯待ち、空き次第の再開を扱う。内勤の既存作業時間と品質式は維持。
3. 配達員を名前で選び、注文一覧の「積む」「外す」「届け先へ」から操作できる。自分で道路をクリックする操作も残す。「1本戻す」「店へ戻る」、任意のおすすめ経路、事故前の予測時間による個別配達との比較。
4. 配達指示にも調理中/梱包済み/積載中/配達中の注文一覧。元の目安までの秒数、遅れと受付残り秒数、配達中の到着予定を表示。キャンセル理由はUIThemeの初期8秒間残す。
5. 未出発注文は元の目安+20秒まで受付。`ScoreManager.lateAcceptanceSeconds`。元のDeadlineは変更しない。調理・選択・積載・出発・取消・自動営業を同じ受付期限へ合わせた。
6. 遅配は配達成功、既存の毎秒5点減点・下限0。遅配のチップ/品質ボーナスなし。閉店の未出発キャンセルと全員帰還後の精算は維持。
7. 道幅・方向矢印は画面上の大きさを制限し、ピン背景と名前の重なりを修正。MapManager/ManualDeliveryRoute/RoadPathfinder/MapDefinitionはこの変更で変更していない。

詳しい変更ファイル・操作・判断は[配達UX結果](../Results/Dispatch_UX_Results.md)。これを、旧設計書中の3区画・手動で箱を閉じる・目安で即キャンセルより優先する。自動梱包は「手作りの取り出しも自動」という意味ではない。手作りでは取り出すタイミングと焦げの遊びを残している。

## コードを読む入口

- `Assets/Scripts/UI/Screens/Business/BusinessView.cs`：2画面切り替え、伝票、通知。
- `BusinessView.Dispatch.cs`：配達員・バッグ・地図の操作、出発、参考時間。
- `BusinessView.Orders.cs`：配達指示の注文一覧、秒数、積み込みボタン、道を戻す/帰路。
- `BusinessView.Kitchen.cs`：手作りの材料・ソース・窯と自動梱包の見た目。
- `CookingManager.cs`：既存の工程/品質/内勤と、プレイヤーの自動梱包タイマー・資源予約。
- `ScoreManager.cs` / `DeliveryScoring.cs`：受付猶予と遅延減点。`HandoverManager`は画面ではなくバッグのモデルとして引き続き使用。
- `OrderListUI.cs` / `DeliveryFlowController.cs`：注文選択・出発/到着/キャンセル。`DaySimulationSession.cs`の自動方針も猶予中の注文を候補にする。
- `UITheme` / `UiKit` / `StickerButton`等：現在の見た目。新UI配下はShopUiRootで旧ThemeDriverの一律装飾から区別。
- 新UIはAwakeで生成。新しいフォント・素材・旧部品への接続済みなので、新規に画面一式を作り直さない。

## 既存の検証結果と限界（実行依頼ではない）

- 最終実装の全3733項目通過：単体/画面784（M3/M4 170、M5 67、reset 8、M6 135、screen 58、M7 82、visual 216、shop 48）、シミュレーション2949（M5 72、M6方針825、M6比較1239、M7 F/G/H 813）。
- 2回の通常実行が一致、12本のCSVのSHA256も一致。[記録](Dispatch_UX_Validation.json)。旧4194より少ないのは早期破産で日ごとの検証行が減ったため。合格条件を緩めたものではない。
- Playの20段階の操作処理・道路の画面座標選択、設備表示2ケースも通過。[操作](Shop_Manual_UI.json)、[設備](Shop_Station_UI.json)。物理マウスによる人間の面白さ確認とは別。
- [before/after画像](../UI_Screens/dispatch-ux/README.md)：1920×1080/1280×720、開店準備・厨房・配達指示・精算の8枚。fixtureの数値をバランス測定値と混同しない。撮影後にGameビュー設定を復元済み。
- F/Gは30日完走、Hは2日破産。既存経営係数は未調整。Hの30秒指定は物理下限約37.81秒へ補正される。
- 最終実装の検証終了時にコンパイル/実行エラーなし。今回の読み取りではPlay中だが、操作した主体は未確認。新しい引き継ぎでは長時間検証を回していない。

## 残作業・保留

店内UI全S2/Dの完了は宣言していない。以下は残作業の情報であり、この文書が一括実装を新しく指示するものではない。

- タイトル/遊び方の新デザイン（遊び方の**文言**は新操作へ更新済み）、部品カタログ。
- 店内をSpriteRenderer/Collider2Dのワールドで扱う計画。現実装はUGUIとパン。既存部品を不用意に全置換しない。
- 手で持っているもののEsc取り消し（右クリック取消は既存）。
- 大規模地図のチャンク/LOD/10倍負荷測定。60fps達成は未確認。
- 任意S2-6、V-5、M4-9、C-5の数値調整は保留。20秒猶予はユーザーの追加依頼に対応した初期判断として実装済み。
- shop→main統合は未実施。引き継ぎだけでmerge/pushしない。

別worktree：`.worktrees/codex-text` / `codex/v4-text` にe99263a（UiStringsの文言と検証）、shopへ未統合。現在のShopStringsとの重複を確認してから取り扱う。`.worktrees/claude-review` / `claude/review-v4` は旧レビュー用で、今回の実装場所ではない。

## Unityと確認ツールの注意

- Claudeに既存のKitWrightネイティブMCPがあればそれを優先。補助は `Tools/Review/unity_call.py`。現在の接続先を使い、ポートを決め打ちしない。認証URLや設定全文を出力・文書化しない。
- シーン/prefab/assetはUnity Editor APIで変更し、必要なものだけ保存。Source編集の前にPlayを終了し、再コンパイル→エラー確認→Play。Play移行の処理完了を待つ。
- 画面の非表示はCanvasGroup。zoneTabsの3スロットは互換性のため維持し、スロット1を非表示にする。Canvasの並び依存は[一覧](../UI_Screens/ScreenInventory.md)。
- 検証fixtureは通常セーブを保護するが、メモリ上の現在の営業を初期化する。ユーザーのPlayを不用意に壊さない。
- `validate_shop_ui.py`は操作全体20段階なので、軽いラベル修正ごとに必ず実行するものではない。関係する確認だけでよい。
- 全検証が必要な区切りでのみ `run_suite.py <label>`。RenderingSuspendedは長時間計算中だけ使用し、finallyで解除する。ゲーム処理は止めない。
- 撮影：`capture_shop_views.py --folder <作業名>`、720pは`--720`。既存beforeを上書きしない。
- Reflectionによる検証Tickのまとめ呼び出しはMCP安全チェックで拒否されたため不採用。安全チェックを無効化/迂回せず通常方法を使う。

設計書と承認モックは `Docs/Design/設計書_画面デザイン.md`、`Docs/Design/設計書_店内シーン.md`、`Docs/UI_Mock/` に存在し、ユーザーのDownloads添付と一致確認済み。新しい配達UXと検証方針の追加依頼を優先する。

過去の文脈は[旧Current全文](Current_Before_Claude_20261023.md)、[旧Workboard全文](Workboard_Before_Claude_20261023.md)、[復旧結果](../Results/Shop_UI_StageResults.md)、`git log`に保持。古い「現在」・担当・検証方針を再採用しない。
