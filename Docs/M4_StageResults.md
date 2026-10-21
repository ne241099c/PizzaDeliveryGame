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
| M4-0 / M4-8 | 未着手 | 指定順に進める |

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
