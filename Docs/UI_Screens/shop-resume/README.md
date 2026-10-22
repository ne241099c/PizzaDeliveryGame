# 店内UI 復旧時の画面（2026-10-23）

全デザイン完成の画像ではなく、Claudeの途中実装を接続・修正した現在の状態。

| 画面 | 1920×1080 | 1280×720 |
|---|---|---|
| 店 | [画像](hub.png) | [画像](../shop-resume-1280x720/hub.png) |
| 厨房 | [画像](kitchen.png) | [画像](../shop-resume-1280x720/kitchen.png) |
| 指示盤 | [画像](dispatch.png) | [画像](../shop-resume-1280x720/dispatch.png) |
| 精算 | [画像](settlement.png) | [画像](../shop-resume-1280x720/settlement.png) |

通常セーブを保護した確認用データ。指示盤は2件を積み、個別64.0秒の参考時間と、選んだ道順との差を表示。精算は少数の注文だけで閉店させた確認用で、営業バランスの評価には使わない。

撮影はTools/Review/capture_shop_views.py。実PNG寸法を検証し、元のGameビューサイズへ復帰。追加した720pの一時サイズも削除。

[作業・検証・残作業](../../Shop_UI_StageResults.md)。タイトル等の新デザイン、地図の見た目の追加調整や大規模化は残っている。
