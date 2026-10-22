# 配達操作の改善：before / after

同じ `capture_shop_views.py` の確認用注文で撮影。通常セーブを保護し、撮影後はGameビューのサイズ設定を戻す。

| 画面 | before（31382d8） | after 1920×1080 | after 1280×720 |
|---|---|---|---|
| 厨房 | [before](../shop-resume/kitchen.png) | [after](kitchen.png) | [after](../dispatch-ux-1280x720/kitchen.png) |
| 配達指示 | [before](../shop-resume/dispatch.png) | [after](dispatch.png) | [after](../dispatch-ux-1280x720/dispatch.png) |
| 開店準備 | [before](../shop-resume/hub.png) | [after](hub.png) | [after](../dispatch-ux-1280x720/hub.png) |
| 精算 | [before](../shop-resume/settlement.png) | [after](settlement.png) | [after](../dispatch-ux-1280x720/settlement.png) |

配達指示には調理中の2件と、配達員に積載した2件を表示する。注文一覧は上下スクロールできる。画像内のスクロール範囲外の注文も保持されている。

検証と判断は [Dispatch_UX_Results.md](../../Dispatch_UX_Results.md) を参照。新タイトル等の店内UIの残作業は今回の完了範囲ではない。
