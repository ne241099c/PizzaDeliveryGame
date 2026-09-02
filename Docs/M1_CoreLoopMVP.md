# M1 コアループMVP 完了メモ

## 完了条件

M1では、以下の流れがPlayモードで動作することを完了条件とする。

1. ダミー注文が画面に表示される。
2. 配達員が画面に表示される。
3. 注文をクリックすると選択状態になる。
4. 配達員をクリックすると選択状態になる。
5. `Complete Delivery` を押すとスコアが増える。
6. 完了した注文がリストから消える。
7. 配達員の選択状態がリセットされる。

## Scene構成

`Assets/Scenes/SampleScene.unity` のHierarchyは、M1時点では以下の構成を基本形とする。

```text
SampleScene
├── Main Camera
├── Global Light 2D
├── EventSystem
├── GameManager
└── Canvas
    ├── ScorePanel
    │   └── ScoreText
    ├── OrderListPanel
    │   └── OrderListContent
    ├── DeliveryPersonPanel
    │   └── DeliveryPersonContent
    └── CompleteDeliveryButton
```

## Script構成

```text
Assets/Scripts/
├── DeliveryFlowController.cs
├── DeliveryPerson.cs
├── DeliveryPersonSelector.cs
├── Order.cs
├── OrderListUI.cs
└── ScoreManager.cs
```

- `Order.cs`: 注文データ。
- `DeliveryPerson.cs`: 配達員データ。
- `ScoreManager.cs`: スコアの保持と表示更新。
- `OrderListUI.cs`: 注文の生成、表示、選択、削除。
- `DeliveryPersonSelector.cs`: 配達員の生成、表示、選択解除。
- `DeliveryFlowController.cs`: 注文、配達員、完了ボタン、スコア加算をつなぐ司令塔。

## Prefab構成

```text
Assets/Prefabs/
├── DeliveryPersonButton.prefab
└── OrderItem.prefab
```

PrefabはScene内で直接編集するのではなく、ProjectビューのPrefabを編集する。

## M2候補

- 配達を即完了ではなく、数秒間の「配達中」状態にする。
- 注文をStart時固定ではなく、時間経過で追加する。
- 注文0件時の表示を追加する。
- スコア以外に完了件数を表示する。
