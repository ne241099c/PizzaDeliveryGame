# Pizza Delivery Game 引き継ぎ資料

更新日: 2026-10-15（最新状況は下記を参照）

> **2026-10-21 現在の状態：M3締め**
> Playでの自由ルート・Renの裏道差を確認済み。判断の深さは時間と期限を入れるM4で再評価する。
> [M3_Closeout.md](M3_Closeout.md) にPlay結果と次の優先順位を記録。
> M3の画面・完了処理を実装後、自由に道を選んでルートを作る方式へ変更。
> 推奨ルートは任意に適用できる1件のみ。固定候補からのプラン選択は廃止。
> 個別配達の参考時間と自作ルートとの差を画面に表示し、完了後も比較結果を保持する。
> 学習用の解説は [M3_RouteCodeGuide.md](M3_RouteCodeGuide.md) を参照。
> 最新の操作方法・検証結果・制約は [M3_ImplementationSummary.md](M3_ImplementationSummary.md) を参照。
> 正式SampleSceneは復旧済み。新画面はPlay開始時に生成される。
> 以下は2026-10-15時点の作業開始前の記録。固定ルート・経路探索なし等の旧方針、Scene復旧状態、未実装一覧は現状と異なる。

## 1. 開発方針

- Unity 6.2 / C# / 2Dで制作する。
- ユーザーはUnityを学びながら進めているため、原則として小さい手順に分けて説明する。
- ユーザーが明示的に実装やコミットを依頼した場合のみ、Codex側で変更する。
- 設計書に沿ってM1、M2、M3の順に進める。

## 2. 現在地

### M1: 完了

- 注文一覧を表示する。
- 注文を1件選択する。
- 配達員を選択する。
- 配達完了で注文を削除し、スコアを加算する。

### M2: 完了

- Store / N1 / N2 / Goalからなる固定マップを表示する。
- N1-N2間の大通りと裏道を切り替える。
- 選択ルートに応じて推定所要時間を更新する。
- `EdgeView`のクリックはInput Systemで入力を受け、`EdgeCollider2D.OverlapPoint`で判定する。
- 詳細は `Docs/M2_MapRouteFoundation.md` を参照する。

最新コミット:

```text
3f30674 M2完了
```

`main`と`origin/main`は、確認時点では同じコミットを指していた。

## 3. M3の目的

M3では次を実装する。

- 2件の注文について、個別配達とまとめ配達の推定時間を比較する。
- 配達員のスキル値を裏道の所要時間へ反映する。
- プレイヤーが配達プランを選び、対象の複数注文を完了できるようにする。

タスク順:

1. M3-0: 注文と配達先ノードの紐付け
2. M3-1: Goal2と近道を追加し、`MapManager.GetTimeBetween`を実装
3. M3-2: 裏道のスキルペナルティを実装
4. M3-3: `OrderListUI`を複数選択へ変更
5. M3-4: `BatchPlanCalculator`を実装
6. M3-5: 比較UIと複数注文の完了処理を接続

## 4. M3-0の作業状況

未コミットの変更として、`Order`に次が追加されている。

```csharp
public string DestinationNodeId;
```

コンストラクタは現在、次の順番で引数を受け取る。

```csharp
public Order(
    string destinationNodeId,
    string destinationName,
    float orderedAt,
    float timeLimit)
```

ダミー注文はM3の検証中、次の2件だけを使用する。

```csharp
orders.Add(new Order("Goal1", "Tanaka House", Time.time, 60f));
orders.Add(new Order("Goal2", "Suzuki Apartment", Time.time, 60f));
```

`Goal3`はM3の対象外であり、マップにもまだ存在しないため追加しない。

## 5. M3の固定マップ仕様

```text
Store -- Main(10/0.8) -- N1 -- Main(10/0.6) -- N2 -- Road(5/1.0) -- Goal1
                              \- Backstreet(13/1.0) -/  \- Road(7/1.0) -- Goal2

Goal1 -- Shortcut(3/1.0) -- Goal2
```

- `Goal1-Goal2`はまとめ配達時だけ使う。
- M3では経路探索アルゴリズムを導入しない。
- 固定マップで必要なノード間の時間を計算できればよい。
- N1-N2の裏道選択は個別配達に反映する。
- まとめ配達は大通りルート固定とする。

## 6. 時間計算

通常エッジ:

```text
時間 = 距離 / (基準速度 * TrafficFactor)
```

裏道エッジ:

```text
時間 = 通常時間 * (1 + (1 - SkillLevel) * BackstreetPenalty)
```

- 基準速度の初期値は10。
- 裏道ペナルティ係数の初期値は1.0。
- ペナルティ係数はInspectorから変更可能にする。

期待値（大通り、SkillLevel 1.0）:

```text
Store -> Goal1: 3.42秒
Store -> Goal2: 3.62秒
Goal1 -> Goal2: 0.30秒
個別配達:        14.08秒
まとめ配達:       7.34秒
```

## 7. Sceneの重要事項

確認時、正式な `Assets/Scenes/SampleScene.unity` には`ScorePanel`しか保存されていなかった。
一方、`Assets/_Recovery/0.unity`にはM2までの次の構成が残っていた。

- OrderListPanel
- DeliveryPersonPanel
- MapRoot
- OrderListUI
- その他M1/M2のオブジェクト

このため、ゲーム画面に`Score: 0`しか表示されない状態になっている。

再開時は最初に次を行う。

1. `Assets/_Recovery/0.unity`をUnityで開く。
2. HierarchyとGameビューにM2までの内容があることを確認する。
3. 問題なければ`Assets/Scenes/SampleScene.unity`として保存する。
4. Playモードで注文2件、配達員、マップが表示されることを確認する。
5. 復旧完了までは`Assets/_Recovery`を削除しない。

Sceneの上書き前には、Unity上で復旧内容を必ず目視確認すること。

## 8. 未コミット・未追跡ファイル

確認時の主な変更:

```text
M  .DS_Store
M  Assets/Scripts/Order.cs
M  Assets/Scripts/OrderListUI.cs
?? Assets/_Recovery.meta
?? Assets/_Recovery/
```

- `Order.cs`と`OrderListUI.cs`はM3-0の意図した変更。
- `.DS_Store`はmacOSの生成ファイルで、通常コミットしない。
- `_Recovery`はScene復旧が完了するまで保持する。
- コミット時は関係するファイルだけを明示的にステージする。

## 9. Unity連携について

前の会話ではUnity Editorを直接操作するツールが公開されておらず、ファイルの確認のみ可能だった。
Unity側では過去に`EditorPipelineServer: ServerStarted`が出ていたが、サーバー起動とCodex側の
ツール接続は別である。

新しい会話では最初に次を確認する。

- Unity操作用ツールが利用可能か。
- 現在開いているSceneを取得できるか。
- Hierarchy、Console、Playモードを取得・操作できるか。

利用できない場合は、スクリーンショットとプロジェクトファイルを使って手順形式で進める。

## 10. 再開直後の完了条件

まずM3-0を完了させる。

- 復旧した`SampleScene`を開ける。
- Consoleにコンパイルエラーがない。
- 注文が2件表示される。
- 注文と配達員を選択して、M1の配達完了処理が動く。
- `Order.cs`と`OrderListUI.cs`の変更を`M3-0`としてコミットする。

その後、M3-1へ進む。
