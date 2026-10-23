# 過去の資料（アーカイブ）

M1〜M6と、置き換えられた計画書（`計画書_見た目.md` は `設計書_画面デザイン.md` に置き換え）の記録。現在の作業の入口ではありません。再開するときは `AGENTS.md` と `Docs/Handoff/Current.md` から読んでください。

シミュレーションの出力先（`Docs/Simulations/M5`、`Docs/Simulations/M6_Adjustment`）はコードが書き込む場所なので、アーカイブせず元の位置に残しています。

## 削除した古いシーン構築スクリプト

`Assets/Editor/` にあった `M4SceneSetup`、`M4StageSceneSetup`、`M5SceneSetup`、`M6SceneSetup`、`M7SceneSetup`、`ScreensSceneSetup`、`VisualSceneSetup`、`ShopSceneSetup` は、各段階でSampleSceneを組み立てた時の記録です。現在のシーンにはもうない旧オブジェクトを前提にしており、実行するとシーンを壊すため、2026-10-23に削除しました。内容はgitの履歴（a9bc3e6以前）で読めます。現在のシーンが正で、新しい画面はプレハブ（例：`Assets/Prefabs/UI/TitleView.prefab` と `TitleViewBuilder`）で作ります。
