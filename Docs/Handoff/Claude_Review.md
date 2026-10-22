> 旧レビュー担当として実施したV-4候補の読み取りレビュー（実施済み）。現在の担当はWorkboardを優先。V-4実装はClaudeが元プロジェクトで行う。

# Claude Code レビュー — V-4候補（読み取りのみ）

更新: 2026-10-22。対象 `ui` d92be84（実装HEADはV-3 5758de0）。worktree `claude/review-v4`。
Unity Editorは開いていない。候補スクリプトは実行していない。コンパイル・Play・全検証は**未実施**で、ここにある内容は「ソースを読んだ結果」に限る。

行番号は次のファイルを指す。
- 候補: `Docs/Handoff/V4_candidate.py.txt`（以下「候補L*」）
- 既存: `Assets/Scripts/UIThemeDriver.cs` など（以下「ファイル名:行」）

---

## 1. 確実な不具合（現行コード、ソースで確認済み）

### 1-1. 主ボタン名が実在しない → 主ボタンが「副」表示になっている
- `UIThemeDriver.cs:13` の `primary` は `OpenBusiness` / `Dispatch` / `Replay` を探す。
- シーン内の実名は `StartBusiness` / `DispatchRoute` / `ReplayDay`（`SampleScene.unity` の m_Name をgrep、`OpenBusiness`・`Dispatch`・`Replay` は0件）。生成元は `M5SceneSetup.cs:19`、`M4SceneSetup.cs:96,106`。
- 結果：営業開始・出発・もう一度遊ぶが Secondary 色のまま。V-1の「主ボタン」要件を満たしていない。
- 修正案：候補L5のとおり名前を置き換える。これは採用してよい。
- Unity確認：Hub/Business/Settlementで3つのボタンがアクセント色・白文字になること。

### 1-2. ボタン内の能力ラベルが本文サイズ(18)になる
- `UIThemeDriver.cs:25` で、Button配下の文字は毎フレーム `SizeFor` で再計算される。
- `SizeFor`（`UIThemeDriver.cs:56`）はButton判定が先なので、`DeliveryPersonButton` 上の `Ability0..5` は、ボタン幅が200以上なら Body になる。`Ability`→Caption の判定（`:58`）に届かない。
- 修正案：候補L16のとおり、`Ability`/`Profile` の判定をButton判定より前へ移す。採用してよい。
- 注意：候補L52でエディタ側から `style.size=Caption` を書いても、実行時は `:25` が毎フレーム上書きする。効くのは `SizeFor` 側の修正だけ。

### 1-3.（小）`danger` 配列の `"Fire"` は一致しない
- `UIThemeDriver.cs:14`。解雇ボタンの実名は `Action`（`M6Validation.cs:24`）。`:46` の「解雇」ラベル判定で危険色になっているので表示上の不具合はない。将来の混乱を避けるため削除するかコメントを付ける程度。

---

## 2. 候補の問題点（取り込む前に直すべきもの）

### 2-1.【重大】CanvasGroupのalphaを `Graphic.enabled` に写す案（候補L4, L9-14, L93）
**結論：現状のままでは取り込まない方がよい。** 必要性も未確認。理由は次のとおり。

1. **他コードが持つ `enabled` と取り合う。**
   `OrderDeadlineView.cs:14` は注文が未割当のとき `image.enabled=false`、割当時に `true` にする（実行順1100で、ドライバ1000より後）。
   - ドライバが最初に見たとき `false` だった帯は `originalGraphicState=false` と記録される（候補L11）。以後ドライバは毎フレーム `false` に戻す。そのあと1100の `OrderDeadlineView` が `true` にするため、**表示画面では毎フレームoff→onが繰り返され、非表示画面でも帯は `true` のまま残る**。「非表示のGraphicはすべてoff」という前提が崩れる。
   - 初回の `enabled` を固定で覚えるため、後からゲーム側が意図して変えたon/offを、ドライバが上書きし続ける。今後の点滅演出なども同じ問題を起こす。
2. **`GetInheritedAlpha` が古い値の可能性がある。** 継承alphaの値は、Canvasの更新（描画前）で確定する可能性が高い。そうだとすると、`LateUpdate` は前フレームの値を読む。
   - 候補L80-81の検証は `router.Show(id,true)` の直後に同じ呼び出しの中で `Refresh()` と判定を行う。古い値で判定されて、**正しく作っても失敗する／誤って合格する**恐れがある。
   - 無効にしたGraphicのCanvasRendererで、親のalphaが1に戻ったときに継承alphaが更新されるかどうかは不明（推測。Unityで確認が必要）。更新されない場合、**一度隠した画面が二度と表示されない**。
3. **TMPのサブメッシュも対象になる。** `TMP_SubMeshUI` もGraphicなので、`GetComponentsInChildren<Graphic>` に含まれる。このフォントは動的・マルチアトラス（`VisualSceneSetup.cs:14-15`）なので、サブメッシュが実行中に生まれる。TMPが自分で管理するものの `enabled` を外から変えることになる。
4. **`RenderingSuspended` 中は写しが止まる。** 長期シミュレーション中に画面が切り替わると、新しい画面のGraphicがoffのまま残る（finallyでfalseへ戻れば、1フレーム後に復帰する）。撮影中に止めていると、画面が空になる。
5. **画面切替のたびに全テキストを作り直す。** 再有効化でTMPがメッシュを作り直すため、切替時に負荷の山ができる。`Refresh` は既に毎フレーム3回の全走査をしており、4回目が加わる。
6. **ImplementationStageで区切られていない。** `>=4` の検証だけが段階で区切られていて、挙動（L9-14）は常に動く。

**原因の候補（推測）**：CanvasGroupのalpha=0では、キャッシュ済みメッシュでも頂点色に0が掛かるので、本来は見えないはず。「隠れたUIメッシュが見える」のなら、次のどれかを先に疑うべき。
- (a) その物体が、隠した画面のCanvasGroupの**外**にある。`HubScreenUI.cs:28` は `DeliveryPolicy`（約束スライダーを含む）を画面切替のたびに付け替えている。
- (b) 撮影方式のせいで起きた表示崩れ。Current.mdには、supersize2やCanvasモード切替でTMPが崩れた記録がある。

**提案**
- まず再現手順を固定する（どの画面、どの解像度変更、どの物体が見えたか）。見えた物体のヒエラルキーパスと、全祖先のCanvasGroup alphaを記録する。
- 原因が(a)なら、親の付け替え先を直す（所有の問題として扱う）。
- それでも必要な場合は `Graphic.enabled` ではなく `CanvasRenderer.cull` を使う案もある。ただしRectMask2Dもcullを書き換えるので、スクロール内では同じく競合する。採用するなら対象を画面ルート直下に限定し、状態を外から触る他コード（`OrderDeadlineView` など）を除外リストに入れる。

### 2-2. 「画面境界」検証が1280×720を検証していない（候補L84-89）
- CanvasScalerは `ScaleWithScreenSize`、基準1920×1080、`Expand`（`SampleScene.unity:23990-23995`）。1280×720は同じ16:9なので、Canvas座標上の配置は**完全に同じ**になる。そのため候補L88の判定は、解像度を変えても結果が変わらない。また `:84` の基準解像度チェックは、ほぼ自明な内容。
- 1280×720で実際に変わるのは**画素の大きさ**。Caption 14は実画面で約9.3px、Body 18は約12px。重なりではなく**読みやすさ**の問題になる。
- 計画書V-4の「重なり・はみ出し」に対して、検証が不足している：
  - 10個の親枠が画面内にあるかしか見ていない。**文字のはみ出し**（TMPの `isTextOverflowing`、または `preferredWidth/Height` が枠より大きいか）と、**兄弟要素どうしの重なり**を見ていない。
  - 提案：表示中の各画面について、`activeInHierarchy` かつ継承alpha>0のTMPで `isTextOverflowing==false`（Overflowモードの文字には `preferredHeight<=rect.height+1`）を確認する。主要な兄弟の矩形（ヘッダ/注文帯/地図/配達員帯/計画パネル）が重ならないことも確認する。スクロール内は除外する。
- 16:9以外（例：Gameビューがフリーアスペクトのまま）では、Expandで余白が出るだけ。Current.mdの注意どおり、`Canvas.pixelRect` とPNGの実寸法を記録すれば十分。

### 2-3. 検証が画面状態を変える（候補L77-83）
- `router.Show` は `Changed` を発火する。購読しているのは `HubScreenUI.cs:20`（`DeliveryPolicy` の親付け替え）と `TitleMenuUI.cs:33`。最後に `Show(previous,true)` で戻しているが、**途中で例外（Check失敗）が出ると戻らない**。`try/finally` で囲むこと。
- 戻すときもフェードはしない（immediate）。フェード中に実行した場合、フェードは打ち切られる。結果は同じだが、記録には残す。

### 2-4. 訓練ポップアップ（候補L55-60）
- `controls.SetAsLastSibling()`：`PreparationUI.cs:47,58` は `row.GetComponentInChildren<Button>()` で、**最初に見つかったButtonを解雇/採用ボタンとして扱う**。TrainingControlsを後ろへ動かす方向なので、今より悪化はしない（`ChooseTraining` は `ScreensSceneSetup.cs:78` で末尾に追加済み）。ただし、この並びへの依存を `ScreenInventory.md` に追記しておくべき。`M6Validation.cs:18-21` は `GetChild(0)` と `TrainingControls/Train0` のパスに依存していて、どちらも維持される。
- ボタンの大きさ：`Train` の文字は2行（`PreparationUI.cs:53` で「訓練 X\n費用」）。200×44のボタンに、候補L41の余白（左右8・上下4）を引くと、文字の高さは36。Captionが14px・2行で、Zen Maruの行間なら約38〜40になり、**はみ出す恐れがある**。Unityで `isTextOverflowing` を確認する。
- 追加したImage（候補L58）は既定で `raycastTarget=true`。非表示中は `blocksRaycasts=false` で問題ない。表示中は下の能力行のクリックを遮るが、ポップアップとしては意図どおり。
- ポップアップの幅は664+8=672。staff 2列の行幅がこれ以上あるかは、ソースでは確定できない（Unityで確認）。

### 2-5. 全ボタンのラベルを引き伸ばす処理（候補L37-42）
- `GetComponentsInChildren<TMP_Text>` で「最初の文字」を選ぶので、ラベルが2つ以上あるボタン（例：注文カード以外のプール行）では、意図しない方を全面に広げる恐れがある。候補は `CourierAbilitiesView` と `CookingCardUI` を除外しているが、`StaminaBar` を持つ配達員ボタンは `CourierAbilitiesView` の有無で判定が変わる。
- 提案：除外ではなく対象を明示する（文字が1つだけのButtonに限る）。

### 2-6. スライダーの見た目（候補L29-31）
- `Background` は高さ4なので、実行時に `ThemedPanel.Apply`（`ThemedPanel.cs:8`、高さ8以下の分岐）が毎フレーム色を `Lerp(background,muted,.4)` で上書きする。候補L30の `theme.muted` は**残らない**。意図した色にしたければ `ThemedPanel` 側で扱う（例：名前 `Background` かつ親がSlider）。
- `Handle` をaccentにする（候補L15）のはよい。ただし `Fill` はgoodのまま（`UIThemeDriver.cs:36`）。約束時間のスライダーは「良い/悪い」の量ではないので、緑の意味がずれる（3-2を参照）。

### 2-7. 解像度の切替（候補L62-73）
- `index=7+found` は「組み込みのサイズが7個」と決め打ちしている。Unityの版で変わる。Current.mdでも720pプリセットは未確認。
- `GameViewSizes` は**プロジェクト外のユーザー設定**に保存されるので、他のプロジェクトにも残る。元の選択を戻す処理もない。
- 提案：変更前の `m_SelectedSizes[0]` を記録し、終わりに戻す。720pの実寸法は、撮影したPNGの寸法で確認する。

### 2-8. その他
- `FinishLayout` は `SetTextRole`・`alignment`・スライダー画像を Undo なしで変更している（`MarkSceneDirty` で保存はされる）。Editor運用の規約（Undo対象）に合わせるなら `Undo.RecordObject` を使う。
- 候補L93の「破棄オブジェクト掃除」は、プール行の再生成のたびに辞書が増え続けるのを防ぐためには必要。ただし毎フレームLINQで配列を作る。2-1を採用しないなら不要。

---

## 3. 提案（不具合ではない、V-4範囲の改善）

### 3-1. 720pでの文字の小ささ
- 2-2の計算のとおり、Caption 14は720pで約9px。読みやすさを撮影で確認し、ユーザーに見せて判断してもらう。数値（UITheme）の変更は見た目の範囲だが、勝手に決めず報告に含める。

### 3-2. 色の意味の一貫性（V-4の自己点検の材料）
| 場所 | 現在 | 意味のずれ |
|---|---|---|
| `UIThemeDriver.cs:36` 名前に `Fill` を含むImageはすべてgood | 能力バー、約束スライダー、`TimeFill` | 能力やスライダーは「良い」ではなく量。`TimeFill` は `DeliveryFeedback.cs:77` で上書きされるが、同じフレームにドライバ（1000）→フィードバック（1500）の順で2回書いている |
| 主ボタン accent `#D8452B` と 危険ボタン bad `#B3261E` | どちらも赤系の地に白文字 | 「営業開始」と「解雇」が似て見える。危険は枠線だけ、または暗い赤にするなどの区別が必要か、画像で確認 |
| 選択中 = accentの淡色＋accent枠 | 主ボタンと同じ系統の色 | 選択と主操作が同じ色系統になっている |
| `DeliveryFeedback.cs:75` 所持金の変化 | ▲good/▼bad | 一貫している（黄=お金は数字の固定色ではなく、増減で色を変える方針と記録する） |
| `DeliveryPersonSelector.cs:93,105,108` | `Color.cyan`、直書きのRGB | UIThemeの外にある直書き。実行時にThemedButton/ドライバが上書きしているので見た目への影響は小さいが、V-4の「直書き」の点検に記載すべき |

### 3-3. ホバーの確認
- `ThemedButton.cs:13`：`selectedColor=highlightedColor`。クリックしたボタンは、EventSystemの選択が残る間ホバー色のままになる。タブで「選択中」種別と見分けにくくなるので、画像で確認する。
- `ThemedButton.cs:17` は毎フレーム `Apply`（`Outline` の取得、色の再設定）。既存のままでよいが、負荷を測るときの対象として記録する。

### 3-4. 撮影のタイミング
- `ReviewFrameCapture.cs` は呼び出しの次のフレームの終わりで撮る。`router.Show` をimmediateにしないと0.3秒のフェード途中を撮る。2-1を採用した場合は、1フレームの遅れでGraphicがoffのまま撮る恐れがある。撮影前に2〜3フレーム待つか、immediateで切り替える。

---

## 4. Unityで確認すべき点（統合担当向けのチェックリスト）

1. 1-1の修正後、主ボタン3つの色（1920/1280の両方）。
2. 1-2の修正後、Hubの配達員カード・営業画面の配達員帯で、能力ラベルが14になり、6行が重ならないこと。
3. 「隠れたメッシュが見える」の再現。見えた物体のパスと、祖先のCanvasGroup alpha。再現しない、または原因が親の付け替えなら、2-1は取り込まない。
4. （2-1を試すなら）Graphicを無効にした後、祖先のalphaを1に戻したとき `GetInheritedAlpha` が更新され、再表示されるか。`OrderDeadlineView` の帯のちらつき。
5. 訓練ボタンの2行が200×44に収まるか。ポップアップが行の幅に収まるか。
6. 1280×720の実PNG寸法と `Canvas.pixelRect`。Captionの読みやすさ。
7. 全検証（Current.mdの順序）。候補の検証を追加した場合、件数が増えることを内訳に書く。

## 5. 資料の整合性

- Current.mdは最終画像を `after/` と `after-1280x720/` としているが、`計画書_見た目.md:163` は `after/` だけを挙げている。2解像度の保存先の決定として、どちらかに明記しておくとよい（矛盾ではなく追加の判断）。
- Workboardでは、Claudeの変更可能範囲がこのファイルだけになっている。AGENTS.mdの「作業の終わりにWorkboardへ記録」と衝突するため、今回Workboardは変更せず、記録は統合担当に任せる。
- Current.mdの「実装HEAD 5758de0」と、実際のHEAD d92be84（資料のみのコミット）は、記述どおり一致している。

## 6. 未確認・限界

- コンパイル、Play、検証、撮影はいずれも行っていない。全検証の合格は推測しない。
- `GetInheritedAlpha` の更新タイミング、TMPサブメッシュの扱い、プール行の実際の幅は、Unityの挙動に依存するので「推測」と明記した。
- シーンYAMLは名前の数を数えるためだけに読んだ。プレハブのインスタンスで名前を上書きしていない物体や、実行時に生成される物体は、grepの件数に入らない。
