# レビュー・再開用ツール

Python 3.11以上。元のUnityプロジェクトで、Unity操作担当者だけが使用する。
Unity操作担当はWorkboardを正とする（2026-10-23の最新交代でClaude Code）。並行実装用worktreeからは実行しない。

- `unity_call.py <tool> ['JSON' | @args.json]`：既存KitWright MCPのツール呼び出し。エラー時は非ゼロ終了。
- **毎回の全検証は不要**。UIは変更箇所の表示/操作、ゲーム処理は関連検証。以下の全検証は大きな区切りか明示依頼時だけ行う。引き継ぎだけで実行しない。
- `run_suite.py <label>`：全単体・シミュレーション検証を順次実行。事前にSampleSceneのPlayと接続を確認。失敗時は停止。
- `capture_views.py <folder>`：`Docs/UI_Screens/<folder>/`へ11画面を保存。事前にPlayと固定解像度を確認。既存ファイルを置き換えるため、フォルダ名は作業ボードで決める。
- `unity_mcp.py`：内部HTTP補助。サーバー起動や設定変更は行わない。

接続先はローカルの `.codex/config.toml` の既存設定を利用する。別のクライアントはネイティブMCPを優先し、必要な場合のみローカル環境変数PIZZA_MCP_URLを設定する。認証付きURLをコマンド、文書、gitへ貼らない。設定の内容をログに出さない。

```sh
python3 Tools/Review/unity_call.py get_editor_state
# Playは事前に確認する。ソース編集後はPlay終了→再コンパイル→エラー確認→Play。
python3 Tools/Review/run_suite.py v4
# 2解像度の実サイズを確認してから、別々の保存先へ撮影する。
python3 Tools/Review/capture_views.py after
```

全検証・撮影の実行済み。最新の配達UXの結果は `Docs/Dispatch_UX_Results.md` を参照。旧ツールのポート固定を引き継がず、現在のローカル設定を利用する。

長期計算中に装飾更新だけを抑えるRenderingSuspendedはfinallyで解除する。撮影は通常セーブ抑制下のfixture操作を含むため、ユーザーが営業プレイ中のときに同時実行しない。


## 店内UIの復旧確認（2026-10-23）

元プロジェクトのSampleSceneをPlayしてから、現在のUnity操作担当が順番に実行する。通常セーブを抑制したfixtureで、メモリ上の営業を初期化する。

```sh
python3 Tools/Review/validate_shop_stations.py
python3 Tools/Review/validate_shop_ui.py
python3 Tools/Review/capture_shop_views.py --folder review-name
python3 Tools/Review/capture_shop_views.py --folder review-name --720
python3 Tools/Review/run_suite.py shop_resume_verified
```

- stations：設備増設とやり直しをフレームをまたいで操作し、古いラベル/窯ゲージが残らないことを確認。
- ui：20段階。画面で使うハンドラーから伝票・調理・窯・自動梱包・注文別の積み込み・地図ズーム・画面座標での道選択・手動ルート・配達・精算・受付猶予とキャンセル理由を確認。マウスの物理入力や、人間による面白さの評価の代わりにはしない。
- capture：店/厨房/指示盤/精算の4画面を撮影する。指示盤に個別配達の参考時間が出ること、PNG寸法も確認。720pの一時Gameビューサイズを最後に削除し、元のプリセットへ戻す。撮影データはfixtureであり、ゲームの数値調整の測定には使わない。
- 長期計算中のRenderingSuspendedは、新UIの描画更新と通知にも適用。ゲーム時計・調理・配達・得点・会計の更新は続く。終了時はfinallyで描画を戻す。
- ソース変更はPlayを終了してから行う。

`capture_shop_views.py --folder <name>` は1920×1080を `<name>`、720pを `<name>-1280x720` へ保存する。beforeの画像を上書きしない作業名を指定する。

Play切り替えのツールがセッションIDを返した場合は完了を待ち、`get_editor_state` でPlay・コンパイル状態を確認してからfixtureや撮影を始める。検証TickのReflection呼び出しはUnity MCPの安全チェックで拒否されるため使わない。安全チェックを無効化せず標準の実行方法を使う。
