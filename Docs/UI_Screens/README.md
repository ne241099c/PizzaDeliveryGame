# 画面比較

## 画面再構成 S-0 → S-5

| 画面 | before | after |
|---|---|---|
| 営業 | [画像](before-screens/business.png) | [画像](after-screens/business.png) |
| 店 | [画像](before-screens/hub.png) | [画像](after-screens/hub.png) |
| 精算 | [画像](before-screens/settlement.png) | [画像](after-screens/settlement.png) |
| タイトル | [旧セーブ選択](before-screens/title.png) | [画像](after-screens/title.png) |

変更前には独立したタイトルはなく、beforeは旧セーブ選択。数値は検証用の同じ初期条件から取得し、通常のセーブは保護した。画面再構成の画像は見た目の底上げV以前のもの。

[実装・検証・判断](../Screens_M7_UI_Results.md)。

## 見た目 V-0 → V-4

| 画面 | before（V-0、1920×1080） | after（V-4、1920×1080） | after（V-4、1280×720） |
|---|---|---|---|
| タイトル | [画像](before/title.png) | [画像](after/title.png) | [画像](after-1280x720/title.png) |
| 店/スタッフ | [画像](before/hub.png) | [画像](after/hub.png) | [画像](after-1280x720/hub.png) |
| 求人 | [画像](before/applicants.png) | [画像](after/applicants.png) | [画像](after-1280x720/applicants.png) |
| 経営 | [画像](before/management.png) | [画像](after/management.png) | [画像](after-1280x720/management.png) |
| 設備 | [画像](before/facilities.png) | [画像](after/facilities.png) | [画像](after-1280x720/facilities.png) |
| 訓練 | [画像](before/training.png) | [画像](after/training.png) | [画像](after-1280x720/training.png) |
| 営業 | [画像](before/business.png) | [画像](after/business.png) | [画像](after-1280x720/business.png) |
| メニュー | [画像](before/menu.png) | [画像](after/menu.png) | [画像](after-1280x720/menu.png) |
| 遊び方 | [画像](before/help.png) | [画像](after/help.png) | [画像](after-1280x720/help.png) |
| 精算 | [画像](before/settlement.png) | [画像](after/settlement.png) | [画像](after-1280x720/settlement.png) |
| ゲームオーバー | [画像](before/gameover.png) | [画像](after/gameover.png) | [画像](after-1280x720/gameover.png) |

beforeとafterは同じ撮影手順（検証用の同じ初期状態、通常セーブを保護）で、同じ11画面を撮った。beforeはV-0時点の旧撮影方式由来の表示の違いを含むため、完全な同条件比較ではない。afterの全22枚は、撮影直前に `VisualValidation.LayoutReport()` で文字のはみ出し・ボタンの重なり・画面外を確認し、問題0件。途中段階は[after-v1](after-v1/)、[after-v2](after-v2/)。判断の一覧は[VisualInventory](VisualInventory.md)のV-4。
