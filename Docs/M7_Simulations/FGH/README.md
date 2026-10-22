> この記録は2026-10-23の配達UX変更後。受付猶予20秒を追加し、自動営業も猶予中の注文を候補に含む。下記の「数値変更なし」は既存の経営係数の調整を行っていない意味。旧仕様との比較時はこの差に注意。

# M7 F/G/H 測定

Seed 20261022。各方針を同じシードで2回実行し全CSVフィールド一致。数値変更なし。

```text
F: days 30, cash 10394.89, total profit 10760.20, day20 8154.71, bankrupt False, late 20, max queue 1, oven utilization 0.271, equipment spending 0.00, maintenance 0.00, requested 60s, actual promise 37.81-60.00s
G: days 30, cash 5848.59, total profit 11671.80, day20 4336.59, bankrupt False, late 42, max queue 1, oven utilization 0.113, equipment spending 5600.00, maintenance 2420.00, requested 60s, actual promise 34.25-60.00s
H: days 2, cash -1.00, total profit 69.00, day20 not reached, bankrupt True, late 14, max queue 1, oven utilization 0.227, equipment spending 200.00, maintenance 0.00, requested 30s, actual promise 37.81-37.81s
```

設備購入は雇用・訓練より先に、窯→バイク→広告(小)→店舗の順で購入可能なものを1日1つ選ぶ。広告を毎日買えるため、広告より下位の店舗拡張に進まない場合がある。Hの30秒指定は往復+調理の物理下限へ補正される。破産で終了した場合は20日目を比較不能と記す。全設備最大時の維持費180/日はC-4の式・実処理で検証済み。目標未達でも数値は調整せず、ユーザー判断待ち。
