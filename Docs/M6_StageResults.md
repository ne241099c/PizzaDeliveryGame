# M6 実装・検証記録

M5をmainへfast-forward統合し、m6を作成。pushしない。式と初期値は計画書_M6.mdを正とする。数値は無断調整せず、A〜Eの測定で提案する。

## M6-0
変更: BusinessEconomy/CampaignManagerの固定費上昇、OrderSpawner/DayStats/DeliveryFlowControllerの発生数・閉店在庫・営業内稼働時間、OrderListUIの複数選択API、DaySimulationSettings/DaySimulationSessionの3方針、M6SceneSetup/M6Validation/M6SimulationValidation、旧シミュレーション検証、SampleScene、仕様資料。
検証: 244既存+5 M6+496自動プレイ=745項目PASS。8組合せ×10日×2回のCSV一致。
判断: 能力合計には積載量も含め、採用可能な候補を日給当たり合計で評価。採用条件を満たす間は上限まで採用。稼働率は営業時間と重なる配達・帰還時間だけで計算し、閉店後の帰還待ちは分母にも分子にも含めない。stockは閉店時に未出発だった件数で期限切れとは区別。

## M6-1
変更: DeliveryPersonに能力の上限・経験値・識別IDとクランプを追加。DeliveryPersonSelector, PreparationUI, CourierAbilitiesView, CourierProgression, M6SceneSetup, M6Validation, 配達員Prefab, SampleScene。
検証: 244既存+30 M6+496自動プレイ=770項目PASS。初期素質、能力設定時の上限、経験値を含むコピー、営業と準備の表示を検証。
判断: 初期名簿の素質は開始時にInspectorの初期素質ボーナス(.25)から設定し、新規ゲームのテンプレートに保持。積載量の上限は2。経験値・素質はシリアライズ対象、営業中の位置・現在の配達は日次状態。

## M6-2
変更: CourierProgression, DeliveryTrip, BusinessEconomy, CampaignManager, DaySimulationSession, M6SceneSetup, M6Validation, 旧シミュレーション検証, SampleScene, CSV。帰還時の5能力の経験値・繰り上がり・上限、精算の成長履歴、CSVの能力/増加量を追加。
検証: 244既存+48 M6+496自動プレイ=788項目PASS。事故停止・無事故道路・遅延/期限内、繰り上がりと余剰、素質上限、日またぎ、当日404→翌日404.8の日給反映を検証。
判断: 日給は営業開始時の名簿と能力で固定し、営業中の成長は翌日から反映する。成長の履歴は能力が実際に増えた帰還単位で記録。採用した日の開始値はその人の最初の出発時に記録する。
