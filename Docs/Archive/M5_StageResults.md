# M5 実装・検証記録

**最新: M5-0〜M5-6完了。337項目PASS。** 操作は [M5_CurrentGuide.md](M5_CurrentGuide.md)。各段階の変更・判断と一時停止の履歴は以下。

現在のm4からm5を作成。main統合・pushは行わない。M4-9の人による数値調整は未実施。

## M5-0
閉店キャンセルの評判低下を独立したSerializeField(初期0)にし、期限切れは従来どおり5。
変更: ReputationManager, DeliveryFlowController, M3Validation.Revision, 計画書・進捗資料。
検証: M3Validation 全169項目通過、コンパイルエラー・警告なし。

## M5-1
変更: CampaignManager / M5SceneSetup / M5Validationを追加。GameClock, DayStats, ScoreManager, OrderSpawner, DeliveryPersonSelector, DeliveryPlannerUI, SampleSceneに日次リセット・次の日の接続を追加。
検証: 169既存 + 10 M5 = 179項目通過。日数・評判・同一名簿の維持、注文・体力・得点・時計のリセットと翌日の営業開始。
判断: シーンを再読込せず同じ名簿インスタンスを保持。新規ゲームだけを別の入口に分離。

## M5-2 初回停止の履歴 (再開後に解消済み)
変更中: BusinessEconomyを追加し初期資金300・到着時の売上加算・日次売上リセットを接続。DeliveryFlowController, DeliveryPersonSelector, OrderListUI, CampaignManager, M5SceneSetup, M5Validation, SampleSceneを変更。
実装中の検証コードにRouteSuggestionをDeliveryPlanへ直接渡す型エラーがあり、ManualDeliveryRoute.BuildCompletionで変換するよう修正。コンパイルエラー・警告は解消済み。
段階検証 M5Validation.Run の内部で、既存M3Validationの「M4-8 results cover policy controls」が失敗。AddEconomyがCanvasの末尾にCashラベルを追加し、DayResultsが最後の兄弟でなくなったことが原因。到着時の所持金・売上計算の新しい検証には到達していない。
計画書の停止ルールに従って、この失敗後は修正・次段階へ進まず停止。M5-2は未コミット、M5-3〜6は未着手。UnityはPlay停止。
判断した点: 金額はdecimalで保持し、初期値はInspectorのfloatから変換する。売上は到着時の確定得点だけを受け取り、閉店キャンセルの得点ペナルティとは分ける。

### M5-2 修正完了
利用者の「直して最後まで」の依頼で再開。AddEconomyの末尾でDayResultsを最後の兄弟に置くよう修正。全169既存+18 M5=187項目通過。期限内チップ・遅延減額を含む実際の到着売上と翌日持ち越しまで検証済み。

## M5-3
変更: DayLedgerを追加。BusinessEconomy, DeliveryTrip, CampaignManager, M5SceneSetup, M5Validation, SampleSceneに材料費即時引落し・帰還時距離の集計・精算処理を追加。
検証: 169既存+31 M5=200項目通過。Aoi220/Ren184、遠回りの距離増、事故停止中の距離不変、損益と所持金の一致、精算の重複防止を検証。
判断: 距離は道路定義の距離を使用し、全員帰還時点で燃料費を確定。通貨は小数第2位・四捨五入(AwayFromZero)。材料費を精算時に二重控除しない。

## M5-4
変更: CampaignManager, DeliveryPlannerUI, DeliveryPersonSelector, M5SceneSetup, M3Validation.Revision, M5Validation, SampleScene。スクロール可能な精算明細・破産表示・通算売上/最高売上・最初からを実装。初期名簿をAoi/Renの2人に変更。
検証: 169既存+39 M5=208項目通過。所持金0は破産しない、マイナス精算と猶予日数、翌日禁止、新規ゲームで初期資金・評判・名簿・体力・記録の初期化。
判断: 猶予は連続したマイナス精算の日数で数え、回復した日は0へ戻す。破産時は精算画面にGAME OVERと通算記録を同時表示し、ボタンをStart overにする。

## M5-5
変更: RecruitmentManager, PreparationUIを追加。CampaignManager, BusinessEconomy, DayLedger, DeliveryPersonSelector, DeliveryPromiseUI, M5SceneSetup, M5Validation, M4PlayValidation, SampleScene。6能力・日給・採用費・解雇手当、2列のスクロール名簿と求人、理由つき制限、日次更新を実装。
検証: 169既存+62 M5+8リセット=239項目通過。固定シード100人の生成規則・再現性・名前重複、資金不足・4人上限・最後1人保護、採用当日の配達・日給・翌日持ち越しを検証。Play画像で文字の重なりと画面下端も修正・再検証済み。
判断: 解雇手当は資金不足でも支払い可能とし、破産は精算時のみ判定(採用は所持金不足で拒否)。採用費・解雇手当は準備費用として別記し、営業損益へ混ぜずに所持金と照合。名前プール不足時は重複を作らず理由を表示する。

## M5-6
変更: DaySimulationSettings, DaySimulationSession, DaySimulationWindow, M5SimulationValidationを追加。OrderSpawner/DeliveryDynamicsに独立したシード指定、DayStatsに事故回数、CampaignManager/DeliveryFlowController/DeliveryMarkerViewに配達中のキャンペーン初期化を追加。M5Validationに閉店後の帰還待ちと配達中のリセットを追加。M3Validation.Revisionを準備中の単独実行にも対応。M5SceneSetup・SampleScene・ガイド・進捗資料・測定CSVを更新。
検証: 169既存+67 M5+8初期化+93自動プレイ=337項目通過。各設定10日上限を2回実行、26日×2回のCSV全フィールド一致。最終実行時エラーなし。準備・精算のPlay画面も確認。
実装中に連続実行の終了処理で一時CanvasGroupの遅延破棄が次の実行と衝突したため、DontSaveの一時部品を再利用し入力状態だけ復元する形に修正。その後全検証を再実行して通過。
判断: 各目安時間の比較は初期資金300・Aoi/Renから開始し、採用/解雇はしない。破産した設定は10日を待たず終了。時計0.25秒刻み、注文は期限順に1件ずつ。実際の推奨出発メソッドを呼び、計算式はシミュレーターに複製しない。終了/中止時は時計・入力・シード設定を復元するが、結果を確認できるようキャンペーン状態は最後に処理した時点に残す。

### シード20261022の測定結果
| 目安 | 実行日数 | 累計売上 | 累計営業損益 | 最終所持金 | 最終評判 | 結果 |
|---|---:|---:|---:|---:|---:|---|
| 30秒 | 6 | 6495.00 | -393.80 | -93.80 | 18.0 | 6日目に破産 |
| 60秒 | 10 | 14865.00 | +3853.00 | 4153.00 | 98.7 | 継続可能 |
| 120秒 | 10 | 9570.00 | +596.40 | 896.40 | 100.0 | 継続可能 |

日別の材料・燃料・日給・固定費・配達/遅延/キャンセル/事故は [CSV](../Simulations/M5/seed-20261022.csv) を参照。30秒は注文が増えても遅延・評判低下で収益が崩れる。60秒はこの固定の1件配送方針では最も利益が高い。人による数値調整や面白さの確認は未実施。

## 段階別の通過数
| 段階 | 全通過数 |
|---|---:|
| M5-0 | 169 |
| M5-1 | 179 |
| M5-2 | 187 |
| M5-3 | 200 |
| M5-4 | 208 |
| M5-5 | 239 |
| M5-6 | 337 |

ManualDeliveryRoute・RoadPathfinder・MapManagerはM5で変更なし。Packages・.DS_Store・.codex・_Recoveryをコミットに含めない。main統合・pushはしない。成長・訓練・セーブ/ロードはM5対象外。

最終の追加UI確認中にUnity接続が切断。シーン・CSVは保存済みで、全337項目のPASSはEditor.logにも記録済み。接続復旧のためEditorを再起動。復旧後、保存済みシーンの244項目を再度通過し、実際の営業開始/翌日ボタンと自動プレイウィンドウも確認。コンパイルエラー・警告なし。Play停止で終了。
