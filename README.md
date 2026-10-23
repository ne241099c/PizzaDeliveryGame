<!-- GameInfo:begin (Tools/Game Info/Sync で生成) -->
# Traveling Pizza Salesman

トラベリング・ピザセールスマン（v0.1.0、ゲーム名は仮）

小さな街のピザ屋の店長になって、ピザを作り、配達員に届け先と道順を指示するゲーム。
<!-- GameInfo:end -->

## 開く

- Unity 6000.2.10f1 でこのフォルダを開き、`Assets/Scenes/Title.unity` を再生する（ビルドの最初のシーン）。
- `Assets/Scenes/Game.unity` を直接再生すると、使えるセーブがあれば続きから、なければ新しいゲームで始まる。
- ゲーム名・バージョンは `Assets/Scripts/Core/GameInfo.cs` の1か所で定義し、Unityのメニュー **Tools > Game Info > Sync** でビルド設定とこのREADMEに反映する。
- テスト：Unityのメニュー **Tools > Pizza Delivery > Run Tests**（Test Runnerと同じ。結果は `Library/TestResults/last.txt`）。

## フォルダ

| 場所 | 中身 |
|---|---|
| `Assets/Scenes` | `Title`（タイトル）と `Game`（開店準備・営業・精算） |
| `Assets/Scripts` | ゲーム本体（asmdef `PizzaDelivery`）。`Core`（名前・シーン・時計）、`Campaign`（日の進行・お金・セーブ）、`Staff`、`Orders`、`Kitchen`、`Delivery`（ルートと配達）、`Map`（街と地図の表示 `Map/Views`）、`UI`（`Kit` 共通部品、`Text` 文言、`Screens` 画面、`Title`）、`Dev`（撮影用） |
| `Assets/Editor` | エディタ専用（asmdef `PizzaDelivery.Editor`）。`Builders`（プレハブ・シーン・ゲーム名の生成）、`Validation`（Play中の検証。分野別）、`Simulation`（自動営業）、`Reviews`（撮影） |
| `Assets/Tests` | Test Runnerのテスト（asmdef `PizzaDelivery.Tests`） |
| `Assets/Prefabs` | `UI`（画面）、`Map`（道・地点） |
| `Assets/Art` | 自作の絵（`Tools/Art` のスクリプトで生成） |
| `Assets/Resources` | 実行時に名前で読む絵（`ShopArt`）とテーマ（`UITheme`） |
| `Assets/Settings` | 描画・入力の設定 |
| `Tools` | `Art`（絵の書き出し）、`Review`（Unityを外から操作する確認・撮影） |
| `Docs` | `Design`（設計書・計画）、`Results`（実装と確認の記録）、`Simulations`（自動営業の出力）、`Handoff`（共同作業の引き継ぎ）、`UI_Mock`・`UI_Screens`（画像）、`Archive`（過去の資料） |

## 資料

- 共同作業の入口：`AGENTS.md`、`Docs/Handoff/Current.md`
- 素材の出典：`Docs/Credits.md`
