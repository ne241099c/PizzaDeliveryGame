# S-0 既存画面の調査

2026-10-22。m4/m5はmainの祖先。m6をfast-forward統合後、M3/M4 169、M5 67、リセット8、M6 135、従来シミュレーション87、8方針745、A〜E/E2比較1117の全2328項目が通過。同じソースでbefore画像を取得。

## 部品を移す対応表

| 現在のCanvas直下 | 新しい所属 |
|---|---|
| Preparation(PreparationUI、名簿、求人、訓練、営業開始) | Hub / 店 |
| Header、Selections、MapTitle、FitMap、MapLegend、MapViewportFrame | Business / 営業 |
| RouteEditor、UndoRoad、ClearRoute、Footer、Activity | Business / 配達計画 |
| DeliveryPolicy | 店の経営タブ / 営業の上帯(同じ部品を移す) |
| Cash、CampaignDay | 共通の上帯 |
| DayResults、ResultCard、ResultText、ReplayDay | Settlement / 精算 |
| SaveChoice、Continue、NewGame | Title / タイトル(現状はセーブ選択だけ) |
| Top/Bottom/Left/RightBackdrop | Businessの背景 |
| 既存なし | GameOver、Help、Menuは表示用の枠・操作を追加 |

## 名前・並び・有効状態への依存

- M6Validation.ValidatePreparationButtons：Canvas直下のPreparation/TeamViewport/TeamContent、ApplicantsViewport/ApplicantsContentを固定パスで参照。カードの先頭GetChild(0)、TrainingControls/Train0、CancelTraining、Actionに依存。
- M3Validation.Revision：Canvas直下のFooter固定パス。角の座標がCanvas内に収まることを検証。DayResultsが親の最後の子であることを検証。
- DeliveryPlannerUI.ShowResults：resultsPanel.SetAsLastSibling。ロード/準備で結果パネルをSetActive切替。
- CampaignManager：preparationPanel.SetActive切替。CampaignSaveManager：choicePanel.SetActive切替。
- PreparationUI：カード内部のProfile、Action、TrainingControls/Train0〜5、CancelTrainingの名前に依存。カード内部は維持。
- DeliveryPersonSelector：StaminaBar/Fill。CourierAbilitiesView：Inspector参照。OrderListUI：動的ボタンのプールに依存。リストやプールの構造は維持。
- scene setupツール：Canvas直下の旧固定パスが多い。旧セットアップの再実行で新画面を壊さないよう、新画面用の専用セットアップを追加する。

画面の非表示だけCanvasGroupへ変更。プールから余った行や非表示テンプレートなど、画面ルート以外の既存の有効状態はそのまま扱う。検証のパス変更では計算条件を変えず、部品名を再帰検索して同じ部品を検証する。結果画面の重なり検証は精算ルート内で維持する。

## 画像

- [営業 before](before-screens/business.png)
- [店 before](before-screens/hub.png)
- [精算 before](before-screens/settlement.png)
- [タイトル相当 before](before-screens/title.png)：旧SaveChoice。変更前には独立したタイトル画面はない。

ユーザーの最新指示によりS-0の報告停止を省略し、続けてS-1へ進める。
