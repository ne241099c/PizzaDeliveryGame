# Credits

- 日本語フォント：Zen Maru Gothic Regular / Bold / Black。Copyright 2021 The Zen Maru Gothic Project Authors。[公式配布元](https://github.com/google/fonts/tree/main/ofl/zenmarugothic)。SIL Open Font License 1.1。原本フォントとライセンス全文を `Assets/Fonts/` に同梱。TextMeshProの動的フォントアセットを作成して使用。
- 既存の英字フォント：Liberation Sans。ライセンスは `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` に同梱。
- 丸角パネル・地図の図形・道・アイコン：このプロジェクト用にUnityの標準機能で作成。外部画像・追加パッケージなし。

- 見出し用フォント：Mochiy Pop One。Copyright 2020 The Mochiypop Project Authors。配布元は同梱OFLに記載の[作者リポジトリ](https://github.com/fontdasu/Mochiypop)。SIL Open Font License 1.1の全文を `Assets/Fonts/MochiyPopOne-OFL.txt` に同梱。動的TMPアセットを使用し、不足字形はZen Maru Gothicへフォールバック。
- 店内の材料・ピザ・道具・設備・アイコン：`Tools/Art/shop_art.py` にある、このプロジェクト用のSVG図形をPNGへ書き出したもの。元のSVG文字列と書き出し処理を同じスクリプトに収録。`Assets/Resources/ShopArt/` に保存し、スプライト参照で差し替え可能。
- タイトル画面の絵（夕暮れの空・街のシルエット・店・電飾・道路）と共通の角丸・丸の画像：`Tools/Art/title_art.py` にある、このプロジェクト用のSVG図形をPNGへ書き出したもの。`Assets/Art/Title/`、`Assets/Art/UI/` に保存。ロゴと店の文字は上記のフォントで表示し、画像には含めない。
