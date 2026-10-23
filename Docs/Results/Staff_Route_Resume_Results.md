# 従業員・配達ルート・再開・閉店後進行の修正

2026-10-23 / Codex / shop。開始点 b590bb6。push・mainへの統合なし。

## 変更

- 従業員：配達員・内勤の合計人数制限を採用処理とセーブ読込から撤廃。人数表示も変更。名前候補を使い切った場合は番号付きの重複しない名前を生成する。多人数でも配達員レーンを読めるよう、最小幅を保ち横スクロールする。
- ルート：三角形を赤い行きの線・青い帰りの線と、進行方向へ動く丸に変更。同じ道路の往復は線を左右にずらす。ドラッグするつまみは丸とグリップ模様。道順や経路探索の計算は変更していない。
- 続ける：RestorePreparationがPreparationEnteredを通知していなかったため、読込後もタイトルに留まっていた。通知を追加し、その日の開店準備画面へ遷移させる。
- 閉店後：実時間からゲーム時間への進行を8倍にする。倍率はGameClockのInspectorで変更可能。閉店をまたぐフレームは閉店後の部分だけ加速し、全員が帰還した後に精算する。

## 検証

Playで関連する検証だけを実施し、計441項目が通過。

| 検証 | 通過数 |
|---|---:|
| StaffRouteResumeValidation（新規） | 34 |
| M4Validation | 36 |
| M5Validation経由のM3/M4 | 170 |
| M5Validation | 67 |
| M7Validation | 83 |
| ShopValidation | 51 |

新規検証は20人の採用、名前枯渇、大人数セーブ、実際の続けるボタン、営業からタイトルに戻った後の再開、閉店をまたぐ時間、帰還予定の維持、精算までを確認。既存検証の旧人数上限の期待値は新仕様へ更新した。初回の関連検証でM7に残っていた6人上限の期待値が失敗したため修正し、上記の関連検証を再実行して全項目通過した。

追加のPlay表示確認では、赤・青のルート、丸のマーカー、三角形の撤去、8人分のレーンと最小幅・スクロール領域を確認し、別フレームで丸の移動も確認。撮影画像も目視確認した。人のマウスによるスクロール・ドラッグの感触は未確認。

- [配達指示画面（8人）](../UI_Screens/staff-route-resume/dispatch.png)
- 新規検証・撮影は保存抑制の中で実行し、片付けも抑制中に実施。通常campaign.json・records.jsonは前後のバイト一致で保護を確認。
- 終了時：Unityは編集モード、コンパイル・更新なし。キャッシュ内のコンソールエラー0件。原因不明だったシーンdirtyは保存していない。
- 全検証・長期間シミュレーションは未実行。M6/M7シミュレーション検証の人数上限に関する期待値のみ更新。

## 判断した点

- 人数は制限しないが、採用費用・日給・準備中のみ採用する条件は維持する。
- 旧店舗拡張は人数枠を買う意味がなくなるため、新Hubの設備購入から除外する。既存セーブの拡張レベル・維持費と互換APIは維持し、既存購入分の返金などは追加しない。
- 行きは最後の配達先まで（配達先同士の区間を含む）を赤、最後の配達先から店までを青とする。
- 閉店後の倍率は8倍。配達速度能力・期限・採点式を変更せず、ゲーム全体の閉店後進行を早送りする。メニュー停止中は進めない。

## 主なファイル

採用・保存：RecruitmentManager、CampaignSaveManager、FacilityManager、PreparationUI、Shop/HubView、ShopStrings。
再開：CampaignManager。早送り：GameClock、Shop/BusinessView。
表示：Shop/BusinessView.Dispatch、EdgeView、UITheme。
検証：StaffRouteResumeValidation、M5Validation、M7Validation、ShopValidation、M6SimulationValidation、M7SimulationValidation。

Packages、.DS_Store、.codex、_Recovery、既存の動的フォント4ファイルは作業コミットに含めない。
