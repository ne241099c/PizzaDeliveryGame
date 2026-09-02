# M2 マップ・ルート基盤 完了メモ

## 完了条件

M2では、以下の流れがPlayモードで動作することを完了条件とする。

1. 固定テストマップが画面に表示される。
2. Store / N1 / N2 / Goal のノードが表示される。
3. 大通りと裏道のエッジが表示される。
4. 初期状態では大通りルートが有効になる。
5. 裏道エッジをクリックすると、N1-N2区間が大通りから裏道に切り替わる。
6. 推定所要時間が `3.42s` から `3.05s` に更新される。
7. もう一度裏道エッジをクリックすると、大通りルートに戻る。

## テストマップ

```text
Store -- Main -- N1 -- Main/Backstreet -- N2 -- Main -- Goal
```

M2では自動経路探索は行わず、N1-N2区間だけを大通り/裏道で切り替える。

```text
Store-N1:          distance 10, TrafficFactor 0.8
N1-N2 Main:        distance 10, TrafficFactor 0.6
N1-N2 Backstreet:  distance 13, TrafficFactor 1.0
N2-Goal:           distance 5,  TrafficFactor 1.0
```

基準速度は `10 distance/s`。

```text
大通りルート: 1.25 + 1.67 + 0.50 = 3.42s
裏道ルート:   1.25 + 1.30 + 0.50 = 3.05s
```

距離だけ見ると裏道の方が長いが、交通量補正によって所要時間は短くなる。

## Script構成

```text
Assets/Scripts/
├── EdgeView.cs
├── EstimatedTimeText.cs
├── MapEdge.cs
├── MapManager.cs
├── MapNode.cs
├── NodeType.cs
├── NodeView.cs
├── RoadType.cs
└── RouteState.cs
```

- `MapNode.cs`: マップ上の地点データ。
- `MapEdge.cs`: ノード同士を結ぶ道データ。
- `RouteState.cs`: 現在有効なエッジ、合計距離、推定所要時間。
- `MapManager.cs`: テストマップ生成、ルート切り替え、所要時間再計算、View生成。
- `NodeView.cs`: ノードの見た目表示。
- `EdgeView.cs`: エッジの線表示と裏道クリック判定。
- `EstimatedTimeText.cs`: 推定所要時間のUI表示。

## クリック方式

`EdgeView` では、`LineRenderer` と `EdgeCollider2D` を同じローカル座標で扱う。

```text
EdgeView Transform位置 = エッジの始点
LineRenderer = Vector3.zero から localEnd
EdgeCollider2D = Vector2.zero から localEnd
```

クリック入力はInput Systemで受け取り、クリック位置が `EdgeCollider2D` 上にあるかを
`edgeCollider.OverlapPoint(worldPosition)` で判定する。

`OnMouseDown()` はUnityの入力設定によって反応が不安定だったため、M2では使わない。
ただし、クリック判定自体はColliderに任せている。

## Debug.Logの扱い

M2-2では計算確認のため `Debug.Log` を使った。
M2-4完了時点では `EstimatedTimeText` で画面上に推定時間を表示できるため、通常実行時の
ログ出力は削除した。

## M3候補

- 実際の注文データと配達先ノードを連携する。
- 配達員のスキルに応じて裏道の時間補正を入れる。
- 複数注文のまとめ配達を扱う。
- 自動経路探索が必要になるかを判断する。
