# レビュー・再開用ツール

Python 3.11以上。元のUnityプロジェクトで、Unity操作担当者だけが使用する。
Claudeのレビュー用worktreeではUnity操作をしない。

- `unity_call.py <tool> ['JSON' | @args.json]`：既存KitWright MCPのツール呼び出し。エラー時は非ゼロ終了。
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

元の一時ツールから移設しPython構文を確認済み。移設後のEditor状態取得は成功。移設版の全検証・撮影は未実行。旧ツールの9085固定を引き継がず、現在のローカル設定を利用する。最後のゲーム検証成功はDocs/Handoff/V3_Validation.jsonを参照。

長期計算中に装飾更新だけを抑えるRenderingSuspendedはfinallyで解除する。撮影は通常セーブ抑制下のfixture操作を含むため、ユーザーが営業プレイ中のときに同時実行しない。
