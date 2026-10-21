# 新仕様M4: 段階別の結果

仕様: `設計書_ゲーム性.md`、タスク定義: `計画書_M4.md`。M4-9の調整は対象外。
既存の未コミットM4は、変更前検証76項目PASS後、`a09e283` の基準コミットへ保存した。以下はその差分。pushはしない。

| 段階 | M3Validation全通過数 | 変更したもの |
|---|---:|---|
| M4-2 | 90 | DeliveryPerson、DeliveryPersonSelector、MapManager、ManualDeliveryRoute、RoadPathfinder、DeliveryTrip、DeliveryPlannerUI、M4SceneSetup、SampleScene、M3Validation、M3Validation.Revision、M4Validationと対応meta |

| M4-3 | 103 | OrderSpawner、DeliveryPromiseUI、M4StageSceneSetup、M4SceneSetup、SampleScene、M3Validation.Revision、M4Validation、M4PlayValidationと対応meta |

## 判断した点

- M4-2: 旧データのSkillLevelは土地勘へ移行する。旧検証用の2引数コンストラクタは運転0.5（速度係数1）として残す。実際の開始名簿は新仕様の4人・6能力をSceneとInspectorに保存。
- M4-2: 経路探索のアルゴリズムは変更せず、道路の時間関数を配達員の能力に対応させるための呼び出しだけ変更。手動経路も同じ時間関数を使用する。

- M4-3: 設定変更時は発注タイマーを最初からやり直さず、経過割合を保って新しい間隔を適用する。既存注文の期限は不変。評判係数はこの段階では1固定。
- M4-3: 下限の「最強」は名簿の最速者ではなく、運転1・土地勘1の理論上の配達員。上限240秒を下限が超える将来のマップでは、物理下限を優先してスライダー上限を引き上げる。
