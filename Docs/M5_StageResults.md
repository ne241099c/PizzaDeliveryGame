# M5 実装・検証記録

現在のm4からm5を作成。main統合・pushは行わない。M4-9の人による数値調整は未実施。

## M5-0
閉店キャンセルの評判低下を独立したSerializeField(初期0)にし、期限切れは従来どおり5。
変更: ReputationManager, DeliveryFlowController, M3Validation.Revision, 計画書・進捗資料。
検証: M3Validation 全169項目通過、コンパイルエラー・警告なし。

## M5-1
変更: CampaignManager / M5SceneSetup / M5Validationを追加。GameClock, DayStats, ScoreManager, OrderSpawner, DeliveryPersonSelector, DeliveryPlannerUI, SampleSceneに日次リセット・次の日の接続を追加。
検証: 169既存 + 10 M5 = 179項目通過。日数・評判・同一名簿の維持、注文・体力・得点・時計のリセットと翌日の営業開始。
判断: シーンを再読込せず同じ名簿インスタンスを保持。新規ゲームだけを別の入口に分離。

## M5-2 — 検証失敗のため停止・未コミット
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
