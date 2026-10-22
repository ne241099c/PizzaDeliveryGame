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

## V-4 仕上げ：解像度・重なり・色の点検

CanvasScalerは基準1920×1080、ScaleWithScreenSize、Expand。1280×720も16:9なので、Canvas座標上の配置は同じで、全体が2/3に縮小される。重なり・はみ出しはCanvas座標で判定し、文字の読みやすさは両解像度の実画像で確認した。

| 問題（V-3時点） | 原因 | V-4の対処 |
|---|---|---|
| 営業画面の背後に店画面の文字やボタンが残る | 地図カメラのviewportが地図枠だけで、枠外の画面が毎フレーム消去されない。前の画面の画素が残る（CanvasGroupの継承alphaは正しく0） | `ScreenClearCamera`（全画面・描画なし・背景色で消去、地図カメラより先に描画）を追加。Camera.mainは地図カメラのまま |
| 営業開始・出発・もう一度遊ぶが主ボタン色にならない | `UIThemeDriver`の主ボタン名が実在しない旧名 | 実名 `StartBusiness/DispatchRoute/ReplayDay` に修正 |
| 配達員カードの能力ラベルが18で重なる | 文字サイズ判定でボタン内判定が能力判定より先 | 能力・BakeQualityを先に小さい文字（14）へ。カード見出しは3行が入る66、能力行は25間隔 |
| ボタンの文字が左上に寄る・はみ出す | ラベルが固定位置・固定幅 | 文字が1つだけのボタンはラベルを全面に広げ中央揃え（Editorで一度だけ配置） |
| 訓練の選択肢が「訓練を選ぶ」に重なる | 選択肢が同じ位置 | 同じ行の能力欄の上に背景付きで表示。兄弟順は変えない |
| 約束スライダーが緑の縦長の塊 | 背景が地と同色、つまみの高さが44 | 4pxの溝と16×16のアクセント色のつまみ |
| 見出しが帯からはみ出す | 名前に「Title」を含む見出しが28 | 注文/配達員帯/道順/進める道の見出しを22 |
| 求人に「訓練を選ぶ」が出る（V-3以前から） | 求人行は訓練の選択肢を無効にするがボタンは残る | 選択肢が無効の行ではボタンを隠す |
| 店のタブ・メニュー等の文字が720pで約9px | 幅200未満のボタンを一律14 | 幅150未満と2行の訓練/推奨ボタンだけ14、他は18 |

文字サイズの判断：Caption 14は720pで約9.3pxになる。能力行、カードの補足、2行ボタンでは14を維持した（行の高さに収めるため）。テーマの文字サイズ自体は変更していない。より大きくするならカードの高さと配置を変える必要があり、ユーザー判断とする。

### 色の意味の点検

| 色 | 意味 | 使用箇所 | 判定 |
|---|---|---|---|
| 緑 good | 良い・余裕 | 期限の帯（余裕）、残り営業時間（閉店前は赤）、所持金/評判の▲、体力（低いと赤）、能力の現在値 | 一貫。能力バーは「現在の力量」で、悪い意味には使っていない |
| 黄 money | お金・注意 | 期限の帯（注意）、チップ、経験値の進み、完璧焼きの範囲 | 一貫。経験値はお金ではないが「伸び」の補助色として残した（V-1の判断を維持） |
| 赤 bad | 悪い・危険 | 期限切れ間近、▼、低体力、閉店前、解雇/最初から/再挑戦/タイトルへ戻る確認 | 一貫 |
| アクセント朱 | 主な操作・選択 | 主ボタン、選択中の枠、選んだ道順、スライダーのつまみ | 危険の濃い赤と明度で区別。主＝白文字の朱、危険＝白文字の濃赤。画像で判別可能と判断 |
| 補助 muted | 補足 | 枠線、細い溝、無効ボタン | 一貫 |

残る直書き：`DeliveryPersonSelector.Refresh/EnsureStaminaBar`の色（シアン等）は、ThemedButtonと`DeliveryFeedback`が毎フレーム上書きするため画面には出ない。ロジック側ファイルなので色の直書きは残した。

未翻訳（文言はCodexの担当）：精算の「Game over」「Growth: none」、メニューの「MENU」「Title」。V-4では文言を変更していない。
