# M7 F/G/H 測定

Seed 20261022。各方針を同じシードで2回実行し全CSVフィールド一致。数値変更なし。

```text
F: days 30, cash 1125.66, total profit 1970.80, day20 1777.42, bankrupt False, late 18, max queue 1, oven utilization 0.777, equipment spending 0.00, maintenance 0.00, requested 60s, actual promise 37.81-60.00s
G: days 30, cash 2130.48, total profit 8435.80, day20 1380.68, bankrupt False, late 17, max queue 1, oven utilization 0.408, equipment spending 5600.00, maintenance 2170.00, requested 60s, actual promise 34.25-60.00s
H: days 5, cash -21.80, total profit 339.20, day20 not reached, bankrupt True, late 28, max queue 0, oven utilization 0.637, equipment spending 300.00, maintenance 0.00, requested 30s, actual promise 37.81-37.81s
```

設備購入は雇用・訓練より先に、窯→バイク→広告(小)→店舗の順で購入可能なものを1日1つ選ぶ。広告を毎日買えるため、広告より下位の店舗拡張に進まない場合がある。Hの30秒指定は往復+調理の物理下限へ補正される。破産で終了した場合は20日目を比較不能と記す。全設備最大時の維持費180/日はC-4の式・実処理で検証済み。目標未達でも数値は調整せず、ユーザー判断待ち。
