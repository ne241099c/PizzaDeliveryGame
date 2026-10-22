# CLAUDE.md

<!-- KitWright Unity managed project skills -->
<!-- KitWright Unity project skill versions: unity-mcp-workflow@1.0.0 -->

# KitWright MCP for Unity Project Guidance

This section is managed by KitWright MCP for Unity for Claude Code. Everything between the begin and end markers is regenerated on each sync; edit outside this block.

## Installed skills

- `unity-mcp-workflow` v1.0.0 - Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects.

## Preferred workflow

- Use KitWright Unity tools for Unity editor state and automation.
- Use `execute_code` for non-trivial Unity orchestration. For new snippets, include `using KitWright.Editor.Tools.Scripting;`, implement `IKitWrightCommand`, and use `ctx.RegisterObjectCreation` / `RegisterObjectModification` / `DestroyObject` so changes participate in Undo and `ctx.Log` for traceable output.
- Confirm the Unity project root, active scene, and real object/prefab/asset path before edits. Treat user-provided object names as hints, not paths.
- Inspect Unity objects through MCP before changing user-named scene or prefab targets. Carry the returned `instanceId` into follow-up calls (`find_method=by_id`) instead of re-resolving by name.
- Tool returns are structured JSON (`{success, message, data}` / `{success: false, code, error, data}`). Branch on `code`, not free-form text.
- Set component fields with `set_component_properties` — it picks up `[SerializeField] private` fields and accepts Object references as `{"fileID": <instanceId>}` or `{"assetPath": "Assets/..."}`.
- Read editor state through `get_selection`, `get_prefab_stage`, `get_tags`, `get_layers`, `get_build_settings`; try `execute_menu_item` before writing ad-hoc `execute_code`.
- Never edit `.unity`, `.prefab`, or `.asset` files with shell text tools or patches; use Unity MCP / Editor APIs for scenes, prefabs, and ScriptableObject assets.
- Save only the scene or prefab assets intentionally modified, then read back exact values.
- With default `core` exposure, use the focused workflow tools. With default `full` exposure, prefer specific MCP tools for simple editor operations.
- `execute_code` refreshes assets and waits for compilation before running. For other tools that depend on freshly compiled code, still call `request_recompile` after external script edits.
- In `execute_code`, null-guard every lookup and return explicit missing path/object/component messages; do not run self-healing fallback loops.
- For Unity object references, do not use `??=` for lazy rebinding; use explicit `if (field == null) field = Resolve();`.
- After code or resource edits, exit Play Mode if needed, call `request_recompile`, `wait_for_compilation`, then read compilation or console errors.
- `request_recompile` is rejected while Unity is in Play Mode. Call `exit_play_mode` first, then retry.
- After `enter_play_mode`, the HTTP server briefly drops while Unity reloads the domain. Poll `tools/list` or `get_reload_recovery_status` until it responds again before issuing the next tool call.
- If domain reload interrupts a request, treat the result as unknown until `get_reload_recovery_status`, compilation checks, and MCP readback confirm it.
- Additional installed skills are available under `.claude/skills/`.

## Project

- Project root: `/Users/soma/Unity/PizzaDeliveryGame`
- Product name: `PizzaDeliveryGame`
<!-- /KitWright Unity managed project skills -->


## このプロジェクトの共同作業（KitWright管理ブロック外）

まず `AGENTS.md`、`Docs/Handoff/Current.md`、`Docs/Handoff/Workboard.md`、`Docs/Handoff/Claude_Resume.md` を読んでください。M3やV-4の古い資料から再開しないでください。

2026-10-23、ユーザーの依頼によりCodexからClaude Codeへ元プロジェクトの実装・Unity操作を引き渡しました。現在は `shop`、ゲーム実装の最新コミットは `43e3cac`。Codexは作業を終了しており、同時にUnityを操作しません。Claudeは実装担当であり、レビュー専任ではありません。

**毎回の全検証は不要です。** UI変更は変更箇所の表示・操作、計算やゲーム処理の変更は関連検証に絞ってください。全検証は大きな区切りか明示依頼時だけ行います。引き継ぎ直後に既存の3733項目や長時間シミュレーションを再実行しないでください。これは最新のユーザー指示で、旧「毎段階で全検証」を上書きします。

既存実装を調べて差分だけを変更し、配達指示・自動梱包・20秒の受付猶予を旧仕様へ戻さないでください。push禁止と除外ファイルはAGENTS.mdに従ってください。現在の担当・Play状態・残作業・設計書の優先順位は最新の引き継ぎを正とします。
