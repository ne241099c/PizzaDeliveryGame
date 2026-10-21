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
