# 新仕様M4: 段階別の結果

仕様: `設計書_ゲーム性.md`、タスク定義: `計画書_M4.md`。M4-9の調整は対象外。
既存の未コミットM4は、変更前検証76項目PASS後、`a09e283` の基準コミットへ保存した。以下はその差分。pushはしない。

| 段階 | M3Validation全通過数 | 変更したもの |
|---|---:|---|
| M4-2 | 90 | DeliveryPerson、DeliveryPersonSelector、MapManager、ManualDeliveryRoute、RoadPathfinder、DeliveryTrip、DeliveryPlannerUI、M4SceneSetup、SampleScene、M3Validation、M3Validation.Revision、M4Validationと対応meta |

| M4-3 | 103 | OrderSpawner、DeliveryPromiseUI、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision、M4Validation、M4PlayValidationと対応meta |

| M4-4 | 109 | DeliveryPerson、DeliveryFlowController、DeliveryPlannerUI、M3Validation.Revision、M4PlayValidation |

| M4-6 | 125 | ReputationManager、DeliveryScoring、ScoreManager、DeliveryTrip、DeliveryFlowController、OrderSpawner、DeliveryPromiseUI、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision、M4PlayValidationと対応meta |

| M4-7 | 148 | DeliveryDynamics、DeliveryPerson、DeliveryTrip、DeliveryFlowController、MapManager、DeliveryPersonSelector、DeliveryPlannerUI、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision、M4PlayValidationと対応meta |
| M4-5 | 153 | DeliveryMarkerView、M3Validation.Revision |
| M4-0 | 160 | DeliveryPersonSelector、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision |
| M4-8 | 168 | DeliveryPlannerUI、MapViewportUI、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision、M4PlayValidationと対応meta、および操作・引継ぎ資料 |

## 判断した点

- M4-2: 旧データのSkillLevelは土地勘へ移行する。旧検証用の2引数コンストラクタは運転0.5（速度係数1）として残す。実際の開始名簿は新仕様の4人・6能力をSceneとInspectorに保存。
- M4-2: 経路探索のアルゴリズムは変更せず、道路の時間関数を配達員の能力に対応させるための呼び出しだけ変更。手動経路も同じ時間関数を使用する。

- M4-3: 設定変更時は発注タイマーを最初からやり直さず、経過割合を保って新しい間隔を適用する。既存注文の期限は不変。評判係数はこの段階では1固定。
- M4-3: 下限の「最強」は名簿の最速者ではなく、運転1・土地勘1の理論上の配達員。上限240秒を下限が超える将来のマップでは、物理下限を優先してスライダー上限を引き上げる。

- M4-4: 道順の編集・プレビューは積載超過でも可能だが、出発は手動・推奨の双方で禁止する。拒否理由をルート欄に表示する。経路の成立条件は変更しない。

- M4-6: チップは期限内だけで、基本点とチップの合計を従来どおり整数へ四捨五入する。接客能力は出発時の値を保存し、配達中のInspector変更で得点が変わらないようにする。
- M4-6: 「キャンセル−5」は閉店時キャンセルにも適用する。得点の閉店減点（初期0）と評判低下は別に処理する。


## M4-7で発生したエラーと修正

検証コードが別アセンブリから `CurrentStamina` のinternal設定処理へ直接代入していたため、CS0200が発生した。指示どおり停止し、ユーザーからの修正依頼後に対応した。
`DeliveryDynamics.ApplyTripFatigue` と `RecoverStamina` を使い、実行時と検証時が同じ体力操作を呼ぶ形へ修正。コンパイルエラー・警告なし、148項目すべてPASSを確認した。

### M4-7で判断した点

- 事故の抽選タイミングは各道路へ入る時点。同じ道路を往復すれば各方向で1回ずつ抽選する。
- 体力は仕様の帰還時一括消耗に合わせ、出発時の現在体力を速度と事故リスクに保存する。帰還中・事故停止中を含む店外時間を消耗時間として扱う。
- 帰還したフレームの余った時間は、実際の帰還時刻から待機回復する。
- 乱数は配達ごとの独立ストリームとし、Inspectorで固定シードを指定できる構成にした。

- M4-5: 移動表示は既存のPositionAt参照をそのまま使い、停止中の赤表示・残り停止秒を追加。事故で変わった同じ到着予定を移動と採点で共有する。

- M4-0: 既存の名簿内容とInspector参照を維持し、表示枠・マスク・スクロールと行高を追加。5人目の表示と選択も検証する。


## 全段階後のPlay確認

営業フロー31項目・やり直し6項目を通過。配達5件、遅延1件、キャンセル2件、459点、評判42.4。
目安30秒では60秒間に4件、180秒では1件の実際の発注を確認。既存期限が不変で、評判低下が頻度へ反映されることも確認。
同じ道順の4人の所要時間差、Soraの体力0.2での低速化、固定シード25による裏道事故・15秒停止・再開・遅延減点と評判低下を確認。
もう一度ボタンによって評判50・体力1・得点0・待機状態へ戻ることを、実際の画面座標クリックで確認した。
最終状態でコンパイルエラー・警告なし、Playの実行時エラーなし。検証用の設定はやり直しで消え、通常の初期値へ戻る。

### M4-8で判断した点

- 結果には、仕様の得点・件数・評判に加え、配達員ごとの積載量と残り体力を表示する。やり直しは既存のScene再読み込みを利用し、名簿の初期能力を保ったまま動的な状態を初期化する。
- 目安設定欄の追加で結果画面の重なり順と画面の縦横比への対応が必要になった。結果を最前面にし、CanvasをExpand設定、地図カメラをScene上のMapViewportFrameへ追従させる。ゲーム数値は変更しない。
- M4-9の釣り合い・面白さの判断は実施していない。Play確認はUIイベントと時刻の制御で場面を再現した動作確認。

最新の遊び方とInspector項目は [M4_CurrentGuide.md](M4_CurrentGuide.md) を参照。
