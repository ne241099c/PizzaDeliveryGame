# M6 実装・検証記録

M5をmainへfast-forward統合し、m6を作成。pushしない。式と初期値は計画書_M6.mdを正とする。数値は無断調整せず、A〜Eの測定で提案する。

## M6-0
変更: BusinessEconomy/CampaignManagerの固定費上昇、OrderSpawner/DayStats/DeliveryFlowControllerの発生数・閉店在庫・営業内稼働時間、OrderListUIの複数選択API、DaySimulationSettings/DaySimulationSessionの3方針、M6SceneSetup/M6Validation/M6SimulationValidation、旧シミュレーション検証、SampleScene、仕様資料。
検証: 244既存+5 M6+496自動プレイ=745項目PASS。8組合せ×10日×2回のCSV一致。
判断: 能力合計には積載量も含め、採用可能な候補を日給当たり合計で評価。採用条件を満たす間は上限まで採用。稼働率は営業時間と重なる配達・帰還時間だけで計算し、閉店後の帰還待ちは分母にも分子にも含めない。stockは閉店時に未出発だった件数で期限切れとは区別。
