# V-0 画面・直書き箇所の調査

S-5とM7完了時点の全画面を同じ16:9・1920×1080で撮影。通常セーブをSuppressSavingで保護し、表示確認用の状態は保存しない。

| 画面 | before | 現状と適用対象 |
|---|---|---|
| タイトル/セーブ選択 | [title](before/title.png) | 旧SaveChoiceの部品。濃い青背景、Liberation Sans、白文字、緑ボタン |
| 店/スタッフ | [hub](before/hub.png) | PreparationUI、2列の既存カード、TrainingPickerUI、能力バー |
| 求人 | [applicants](before/applicants.png) | PreparationUIの既存カードと採用理由 |
| 経営 | [management](before/management.png) | HubScreenUIの履歴、DeliveryPromiseUIのスライダー |
| 設備 | [facilities](before/facilities.png) | FacilityScreenUIの4カード。22px説明、緑ボタン |
| 訓練 | [training](before/training.png) | 既存TrainingControlsの選択表示。小さい費用表示 |
| 営業/経路作成 | [business](before/business.png) | OrderListUIの20px、配達員15px/補足14px、地図、右の計画パネル |
| メニュー | [menu](before/menu.png) | TitleMenuUIのCanvasGroup重ね表示 |
| 遊び方 | [help](before/help.png) | TitleMenuUIの4ページ、英語の段落 |
| 精算 | [settlement](before/settlement.png) | SettlementScreenUI、既存DayResults、実際のDayLedgerの値 |
| ゲームオーバー | [gameover](before/gameover.png) | 到達日と成績・能力、再挑戦とタイトル |

## 直書き箇所

- シーン/Prefab：旧Imageの濃紺/青灰/ミント、TMPの白、14/20/22/24px等。旧既定フォントはLiberation Sans。
- `OrderListUI.UpdateLabel`：20px、濃い本文色、選択黄色と期限接近オレンジ。
- `DeliveryPersonSelector.Refresh/EnsureStaminaBar`：15px、選択シアン、待機白、移動灰、体力緑。
- `CourierAbilitiesView`：能力値/素質/XPの計算と配置は既存のまま。Prefabのバー色をテーマへ寄せる。
- `NodeView`：店赤・配達先マゼンタ・交差点白、4.5のラベルと位置。
- `EdgeView.SetRouteStyle`：道の色/太さの直書き。V-2で表示層だけ変更。
- `DeliveryMarkerView`：土地勘による色と5.5の文字。V-2/V-3で人物ごとの識別色/停止表示へ。
- 動的文言：PreparationUI/HubScreenUI/DeliveryPromiseUI/SettlementScreenUI/TitleMenuUI/FacilityScreenUI等。V-1の共通文字表を表示層に適用する。

## 依存と配置

S-0の[依存調査](ScreenInventory.md)を引き継ぐ。DayResultsはSettlementRoot内の最後の兄弟。既存の名前・ボタンのイベント・プールを維持。新しい画面ルートを作り直さない。テーマ適用は既存部品への追加。表示切替はCanvasGroup。

## V-0の土台

`UITheme`に色、4種の文字サイズ、8/16/24の余白、角丸8、地図の線幅・人物色を集約。`ThemedText/ThemedButton/ThemedPanel`で参照。ボタンの主/副/危険/無効/選択中を定義。Zen Maru Gothicの動的TMPフォントとライセンスを同梱。V-0では既存シーン/Prefabにテーマ部品を付けない。検証用の一時オブジェクトで、テーマ変更が反映されることを確認する。
