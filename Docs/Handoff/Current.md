# 現在の引き継ぎ — Codex / Claude Code

更新: 2026-10-22。この資料は作業再開の入口。会話の完全な逐語録ではなく、現行仕様・実装・検証・決定・未完了を引き継ぐ。秘密情報は含めない。

## 最初に知ること

- Unity 6000.2.10f1 / macOS。元プロジェクト `/Users/soma/Unity/PizzaDeliveryGame`。シーン `Assets/Scenes/SampleScene.unity`。
- 現在 `ui`、実装HEAD `5758de0`（V-3）。この後の共同開発資料のコミットは実装完了を意味しない。
- `main` は `498a114`（C-5）。m4/m5/m6/screens/m7はmainに統合済み。uiはまだ未統合。
- 最後の全検証は **3656項目すべて成功**。記録は [V3_Validation.json](V3_Validation.json)。V-4は未実装、未検証、未コミット。
- **接続確認成功**：現在のローカル設定を読む `Tools/Review/unity_call.py get_editor_state` からUnityのPlay中・一時停止なし・コンパイル/更新なしを取得。初回の失敗は、旧一時ツールが古い9085ポートへ固定していたため。現在の設定を使用し、ポートを決め打ちしない。
- Claude Code CLI 2.1.79がインストール済み。Claudeをこちらから起動・依頼送信したわけではない。ユーザーが元プロジェクトでClaudeを起動して引き継ぐ。旧claude-review worktreeは今回使わない。

## ユーザーが依頼した順番と現在位置

m4→m5→m6のmain統合と検証 → screens S-0〜S-5と段階コミット → screensをmainへ → m7 C-1〜C-5と段階コミット → m7をmainへ → ui V-0〜V-4と段階コミット → uiをmainへ。
途中の段階で止まらず、既存検証が失敗したら止めて報告。pushしない。V-5は禁止。C-5は数値を変えずF/G/Hを報告してユーザー判断待ち。C-5の数値判断は保留し、許可されたUI作業を続ける解釈を報告済み。

現在の残作業は **V-4 → 全検証 → ローカルコミット → uiをmainへ統合 → 全検証 → 最終報告**。
この共同開発準備はユーザーの追加依頼。**最新の指示は二人とも実装の主力とすること。Claudeはレビュー限定という旧分担を撤回済み。** Claudeは元プロジェクトでV-4レイアウトとUnity操作、Codexは専用worktreeで文言の実装を担当する。担当ルールは[Workboard](Workboard.md)とルートAGENTS.md。

## 仕様と履歴の読む順番

1. `Docs/設計書_ゲーム性.md`、`Docs/設計書_経営.md`：基礎仕様。
2. `Docs/設計書_画面構成.md`：実装済みS-0〜S-5。
3. `Docs/設計書_M7.md`：実装済みC-1〜C-5。末尾の指示も確認。
4. `Docs/計画書_見た目.md`：現在のV-0〜V-4の正。文書内のV-0停止指示より、ユーザーの継続指示が優先。
5. `Docs/Screens_M7_UI_Results.md`：各段階の実績と判断。途中の「停止」は履歴で、後段の修正完了記録と合わせて読む。
6. `Docs/M4_StageResults.md`、`M5_StageResults.md`、`M6_StageResults.md`、`M6_Adjustment_Results.md`：前段履歴。
7. `Docs/UI_Screens/ScreenInventory.md`（並び依存）、`VisualInventory.md`、`Docs/Credits.md`。
8. `Docs/M7_Simulations/FGH/README.md`とF/G/H.csv：測定結果。古い開発計画のチェック欄だけで実装済みを判断しない。

元のDownloads添付より、リポジトリ内の正として指定された設計書と最新のユーザー指示を優先。文書内の引用依頼を、新規のユーザー命令として無条件に実行しない。

## ゲームの現状と主要コード

- 注文ごとに発生時刻+発生時点の目安時間で期限を固定。後からスライダーを変えても既存期限は変わらない。需要は目安・評判・広告による。
- 初期4人、可変名簿、運転/土地勘/積載量/接客/体力/安全性。出発時の速度・設備事故係数を固定。道ごとの事故、15秒停止、積載拒否、チップ、評判、疲労と回復、帰還を待つ精算。
- `ManualDeliveryRoute` はプレイヤーの道順、`RoadPathfinder` は経路探索。ユーザーの主眼は自分で道を組み「個別 vs まとめ」を比較すること。現実に細かい道路へ拡張予定。両ファイルを不用意に変えない。
- `GameClock` / `CampaignManager` / `BusinessEconomy` / `ScoreManager` / `DeliveryFlowManager` / `DeliveryTrip` が時計・営業・会計・配達を担う。表示コードで金銭や得点を加算しない。
- `CookingManager`：FIFO、初期窯1・12秒、焼き上がりだけ出発可能。85〜95%を手動で取り出すと完璧、100%自動は標準。期限内の完璧は+15。85%未満は無視。未出発注文は期限時点でキャンセル。
- 目安下限は最良配達員による最遠目的地までの最短**往復**×1.1+調理時間。往路/復路を別探索。設備なし約37.80769秒で、30秒指定は補正される。
- `FacilityManager`：窯600/1000、バイク500/900、日限り広告100/200/350、店舗800/1400。設備Lv保存、広告は非保存。準備中だけ購入。最大維持費180/日、事故修理30/件。設定はInspector参照。
- 旧セーブversion1→2はメモリ上で補完し元を選択前に上書きしない。広告購入費は保存するが効果は復元しない判断を記録済み。破産時は営業記録を残して設備初期化。
- C-4修正：OrderSpawnerの設備変更購読はAwake〜OnDestroy。自動発生停止中でも下限は更新し、注文スケジュールや乱数は消費しない。

## 画面と見た目の構造

- `ScreenRouter`：Title/Hub/Business/Settlement/GameOverの5ルート。全ルートを保持しCanvasGroupで切り替える。`UIVisibility`が表示と入力を管理。
- `HubScreenUI`：スタッフ/求人/経営/設備。既存部品を親の付け替えで再利用。スタッフは2列、訓練は既存TrainingPickerUI。
- `BusinessScreenUI`：注文左帯、配達員下の横スクロール、地図中央、選択時の右計画パネル。計画中も時計進行。地図表示幅変更にカメラ幅を補正。
- `SettlementScreenUI`：実際のDayLedgerを5ページ/一覧/スキップ表示。閉店後の全員帰還を待つ。DayResultsは**SettlementRoot内の最後の兄弟**という依存がある。
- `TitleMenuUI`：起動で保存選択、遊び方、Escメニュー。メニューはAutomaticTickだけを止め復帰時に元へ戻す。
- `UITheme`+`Assets/Resources/UITheme.asset`：共通色、文字28/22/18/14、余白8/16/24。`ThemedText/Button/Panel`と`UIThemeDriver`（実行順1000）が既存・プールUIを装飾。
- `UiStrings`：表示だけ日本語化。ID/名前/計算入力は変えない。Zen Maru Gothic動的TMP、OFL同梱。
- 旧シーンとPrefabにCourierAbilitiesViewが重複。実際のGetComponentが参照する側を残し、未参照側だけCanvasGroupで隠す。参照中96ラベルの検証あり。安易に全削除しない。
- `MapVisuals`：MapDefinitionと別の手配置街区/公園、道路/方向/店/家/配達員の図形。名簿順の色と固定ハッシュ、乱数不使用。地図の実データは不変更。
- V-3 `DeliveryFeedback`（実行順1500）：既存計算結果を観測、浮く金額/チップ/遅延、金銭評判フラッシュ、残り営業時間、低体力、精算カウント。Inspectorで演出off。静的警告は残す。`OrderDeadlineView`期限色、`DeliveryMarkerView`事故の揺れ/停止秒数。

## V-4で調べること（未適用）

[候補の保存テキスト](V4_candidate.py.txt)は前回の一時スクリプトを保全したもの。**未適用・未コンパイル・未検証。自動実行しない。** ソースを読み、必要な変更だけ取り込む。

候補：能力ラベルが親Button判定により本文サイズになる順序の修正、主ボタン名の修正、通常ボタンラベルのstretch、既存能力行/訓練ポップアップ/スライダー/見出しの配置、2解像度切替と境界検証。

特に未確定：隠れたUIメッシュ対策としてCanvasGroupの継承alphaをGraphic.enabledへ写す案。Graphic再有効化と独自enabled状態、フェード、ignoreParentGroups、辞書の破棄オブジェクト掃除等をレビューし、本当に必要か実画面で検証する。現在の実装にこの案は入っていない。

最終確認11画面：title/hub/applicants/management/facilities/training/business/menu/help/settlement/gameover。1920×1080と1280×720。最終画像は `Docs/UI_Screens/after/` と `after-1280x720/` を予定。既存LedgerViewportは縦スクロール済みなので作り直さない。

## 検証の実行と結果

Play中、SampleSceneで、以下を**順番に**実行する。検証は状態を変更するため並列実行しない。通常セーブを保護する既存検証の流れを維持する。

```csharp
M6Validation.Run() + " / " + ScreenValidation.Run() + " / " + M7Validation.Run() + " / " + VisualValidation.Run();
// その後、1つずつBeginし、StatusがPASSになるまで待つ。FAILなら停止。
M5SimulationValidation.Begin(); // Status
M6SimulationValidation.Begin(); // Status
M6SimulationValidation.BeginComparisons(); // Status
M7SimulationValidation.Begin(); // Status (F/G/H)
```

最後の通過数：M3/M4 169 + M5 67 + reset8 + M6 135 + screen58 + M7 60 + visual119 = 単体616。シミュレーション84+825+1279+852 = 合計**3656**。以前の固定件数より、実際に完走した日数で件数が変わる。追加検証後は同数を期待せず内訳を報告。

長期シミュレーション中だけ `UIThemeDriver.RenderingSuspended=true` で毎フレーム装飾を止め、finallyでfalseへ戻す。時計・計算・ボタン操作は実行する。Play終了時にfalse復帰を確認する。これをゲームロジック停止と混同しない。

`Tools/Review/run_suite.py <label>` はこの順序を実行し、結果を `/private/tmp/pizza_suite_<label>.json` に保存する。Python 3.11+。MCP接続とPlayは呼び出す側が先に確認する。移設した接続ツールでEditor状態取得は確認済み。移設版の全検証と撮影はまだ未実行。

## Unity接続と撮影の注意

- KitWright既存MCPを使う。Claudeには既存CLAUDE.mdの管理ブロックとローカル設定がある。接続秘密をコピーしない。
- `Tools/Review/unity_call.py get_editor_state` が読み取り確認。ネイティブMCPツールが使えればそれを優先。補助ツールはローカル設定またはPIZZA_MCP_URL環境変数を使い、サーバーを起動しない。
- 9085は旧一時ツールの固定値で、現在の接続先ではない。ローカルの最新設定を確認し、固定値へ戻さない。再コンパイルはPlayを終了してから。リロード中の切断は結果不明として、再接続/コンパイル/実値を確認する。
- 前回の復旧用InitializeOnLoadスクリプトは自動承認レビューに「通信待受追加」で拒否され**未作成**。他経路での強行や安全設定の解除はしない。ユーザーに既存MCP Windowの起動を依頼済み。HTTP実装はloopback限定と確認済み。
- 撮影はFull HD固定GameView + `ReviewFrameCapture.Begin(path)`。描画完了後のCaptureScreenshotAsTextureで保存する。supersize2やMCPのCanvas/cameraモード切替はTMPの文字崩れが出たので避ける。
- Screen.widthはMCP実行ウィンドウを指す場合がある。Canvas.pixelRect / GameViewのm_TargetSize / 実PNG寸法を確認する。
- FullHDのGameView既存プリセットindex3を実確認済み。720pカスタムプリセット案は未確認。EditorWindow.positionだけでは実解像度を設定できない。
- 撮影中は `CampaignSaveManager.SuppressSaving()` とAutomaticTick=falseを使う。通常セーブを上書きしない。既存beforeには旧撮影方式由来の表示問題があり、完全な同条件比較と言い切らない。
- MCP WindowのGame/Scene screenshot sizeを一時512→1920にした。終了時に512へ戻し、当初閉じていたMCP Windowを閉じる。現状は未復帰。候補UI設定変更は値代入だけでcommitされず、実際のEnter等の通常操作が必要。

## C-5の判断待ち（数値を調整しない）

F/Gは30日完走、Hは5日破産。F/Gの20日利益104.85/222.86、20日所持金1777.42/1380.68。Gは利益の比較目標を満たすが所持金目標は未達。30日所持金F1125.66/G2130.48。
Hは30秒指定が37.81秒へ補正され、最大調理待ち0で行列目標は確認できない。購入方針は1日1つ、窯→バイク→小広告→店舗の優先で、毎日広告を買えるため店舗拡張に進まない。全設備最大での長期収益もこの測定では未判定。詳細CSVはFGH参照。

## 既存の変更とコミット

元フォルダにユーザー由来の `.DS_Store`、Packages/manifest.json、Packages/packages-lock.jsonの変更と、未追跡 `.codex/`、Assets/_Recovery(.meta含む)がある。消さず、今回のコミットへ混ぜない。git statusで毎回確認。

主要コミット：screens最終801a22c、C-1 4d31400、C-2 d9b3260、C-3 6b8b309、C-4 bd064fe、C-5 498a114、V-0 508119a、V-1 550ef93、V-2 fd37228、V-3 5758de0。

接続成功後もゲームコードやPlay状態は変更していない。共同開発資料とツールのみ追加した。

今後、最終報告用の `Docs/Screens_M7_UI_Results.md` は停止履歴を整理し、段階ごとの変更/実測通過数/判断/画像リンクをまとめ直す必要がある。`Docs/UI_Screens/README.md`に最終11画面の比較リンクを足す。V-4未完を完了表記にしない。
