# M6 実装・検証記録

> 更新：T-1報告後の続行指示により数値調整を実施。現在の値・検証・再測定は [M6_Adjustment_Results.md](M6_Adjustment_Results.md)。本書の初回測定・未適用という記述は調整前の記録です。

**M6-0〜M6-6実装完了。最終2042項目PASS。数値は初期値を維持。** 操作は [M6_CurrentGuide.md](M6_CurrentGuide.md)、目標との差と提案は [M6_BalanceReport.md](M6_BalanceReport.md)。

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

## M6-3
変更: TrainingManagerとTrainingAssignmentを追加。BusinessEconomy, CampaignManager, RecruitmentManager, DeliveryPersonSelector, DeliveryFlowController, PreparationUI, M6SceneSetup, M6Validation, SampleScene。
検証: 244既存+67 M6+496自動プレイ=807項目PASS。訓練費、資金・素質・同日2回制限、取消全額返金、解雇禁止、半日/全日完了、成長後の選択と日給を検証。
判断: 能力別Trainボタンで能力選択と指示を同時に行う。開始前の取消は1日1回の枠を戻す。訓練中は配達の稼働率へ含めない。日給は他の成長と同様に翌日から上昇。

## M6-4
変更: RecruitmentManager, DeliveryPerson, PreparationUI, M5Validation, M6Validation, CSV。新人/即戦力/標準の3タイプ、タイプ別能力/素質/積載量、素質の平均差による採用費、タイプと得意能力の表示を追加。
検証: 244既存+80 M6+496自動プレイ=820項目PASS。固定シード2000候補のタイプ閾値・比率・範囲、伸びしろの価格、素質保持と既存の採用/配達を検証。
判断: 新人型以外の積載量素質の確率は指定がないため、1件の候補でも積載量素質は2とする。新人型のみ指定どおり70%で2。タイプ名は既存英語UIに合わせNewcomer/Experienced/Standardで表示。

## M6-5
変更: CampaignSaveManager/CampaignSaveData/ApplicantSaveDataとM6SaveValidationを追加。CampaignManager, BusinessEconomy, ReputationManager, DeliveryPerson/Selector, RecruitmentManager, TrainingManager, OrderSpawner, DaySimulationSession, M5Validation, M6Validation, M6SimulationValidation, M6SceneSetup, SampleScene。
検証: 244既存+100 M6+497自動プレイ=841項目PASS。JSONと一時ファイルの往復、候補・成長・XP・訓練の一致、準備時保存、営業中非保存、起動時選択、形式不正/旧版での安全な新規開始、破産削除、通常セーブが検証/自動プレイで変わらないことを確認。実装中のout引数をラムダで参照するコンパイルエラーを修正後に全項目通過。
判断: 準備に入った時だけでなく準備中の雇用/解雇/訓練/取消/目安変更と営業開始直前にも保存し、指示を失わないようにする。営業中の再開は最後の開店準備へ戻る。所持金と準備費用はdecimal文字列で保存。求人は引き直さず復元。将来の乱数列までは保存せず、保存時点の候補を保持する。保存先はpersistentDataPath/campaign.json。

## M6-6
変更: DaySimulationSettings, DaySimulationSession, DaySimulationWindow, CourierProgression, TrainingManager, PreparationUI, M6Validation, M6SimulationValidation。訓練方針、訓練先/費用・経験のみの成長/素質のCSV、A〜E比較、CSVエスケープ、実際の準備ボタン検証。DeliveryDynamics/RecruitmentManager/M6SaveValidation/M6SceneSetup/SampleSceneで保存求人の採用時体力初期化も追加。ガイド・進捗資料・比較CSVとバランス報告を追加。
検証: 244既存+106 M6+657既存8方針+1035 A〜E=2042項目PASS。8方針×10日×2回、A〜E合計128日×2回でCSV一致。通常セーブ不変。コンパイルエラー/警告なし。準備画面の訓練ボタンと取消表示もPlayで確認。
最終UI確認で、非表示の準備画面が名簿更新イベントを受け取れず古い人物を参照する不具合を発見。再表示時にRefreshし、訓練/取消/解雇/採用の実ボタンの回帰検証を追加した。その後計算・両シミュレーションを再実行して全項目通過。
判断: 自動方針は雇用の後に訓練を選び、資金条件を満たす最も低い能力を1日1人だけ指示。A〜E全て経験成長は有効。経験だけの成長と訓練込みの増加量を区別して出す。成長件数は能力が実際に増えた履歴数。M6-6は数値を勝手に変えず、目標未達の理由と候補値を報告する段階とする。

### 比較結果
| 方針 | 日数 | 最終所持金 | 訓練回数 | 結果 |
|---|---:|---:|---:|---|
| A 60秒固定・1件・雇用なし | 30 | 10489.40 | 0 | 継続 |
| B 目安調整・1件・雇用なし | 30 | 5584.60 | 0 | 継続 |
| C まとめ・雇用・目安調整 | 30 | 11987.74 | 0 | 継続 |
| D C+訓練 | 30 | 6856.84 | 20 | 継続 |
| E 30秒固定・訓練方針 | 8 | -161.40 | 0 | 破産 |

Aが30日でも黒字、BがAを下回る、Dの訓練費が回収できない、Eは予備金条件で訓練できない、土地勘のXPがほぼ発生しない等の未達を報告。案の固定費増加25、訓練費低下や時間短縮、予備金100、経験係数等は**未適用**。[測定と提案](M6_BalanceReport.md)参照。

## 段階別の結果
| 段階 | 全通過数 |
|---|---:|
| M6-0 | 745 |
| M6-1 | 770 |
| M6-2 | 788 |
| M6-3 | 807 |
| M6-4 | 820 |
| M6-5 | 841 |
| M6-6 | 2042 |

M5をmainへfast-forward済み。そのmainからm6を作成し、M6を7段階でローカルコミット。M6のmain統合・pushはしない。Packages/.DS_Store/.codex/_Recoveryは含めない。ManualDeliveryRoute/RoadPathfinder/MapManagerは変更なし。

保存求人の現在体力はJSONで保存しない日次状態のため、復元した候補を採用する時にDeliveryDynamicsのInspector初期値へ戻すよう修正。通常候補とロード候補の採用を同じ処理にし、実ファイル往復後の採用でも満タンになる検証を追加した。

最終状態: 全2042項目PASS、直近の実行時エラーなし。Unity接続あり・Play停止。Inspector保存値は経験200/成長0.02、訓練比率0.5、採用の伸びしろ係数400、固定費増加15で、提案値には変更していない。
