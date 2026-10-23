using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
/// <summary>Presentation-only Japanese string table; persistent IDs and calculation inputs stay unchanged.</summary>
public static class UiStrings
{
    public const string ReserveHint="焼く前の注文も、クリックかドラッグで配達員へ予約できます。";
    public const string ReserveShortHint="焼く前でも予約OK・下のレーンへ";
    public const string DepartureWaiting="準備待ち";
    public const string WaitingForBaking="焼き上がり待ち";
    public const string NextSection="次の区間へ";
    public const string FinishRoute="編集完了";
    public static string SlowPlanning(float rate)=>$"道順を計画中 ×{rate:0.##} ／ 右クリックでも終了";
    private static readonly Dictionary<string,string> phrases=new()
    {
        {"Click a road connected to the current node (*).\nBlue: next / Green: used / Yellow: last\nScroll: zoom / Middle-drag: pan","現在地（*）から道をクリック / 赤：自分の道順 / ホイール：拡大縮小 / 中ボタンのドラッグ：移動"},
        {"1 / ORDERS (select up to 2)","注文（2件まで選択）"},{"COURIERS / select an available courier","配達員 / 待機中から選択"},{"3 / YOUR ROUTE","自分の道順"},{"Undo last road","道を1つ戻す"},{"Clear route","道順を消す"},{"Dispatch with suggestion","推奨ルートで出発"},{"Dispatch","出発"},{"Reset","やり直し"},{"Fit","地図全体"},{"OPEN","営業中"},{"Time","所要時間"},{"Ability / Potential / 経験値","能力 / 素質 / 経験値"},{"Ability / Potential / XP","能力 / 素質 / 経験値"},
        {"Click a road connected to the current node (*).","現在地（*）につながる道をクリック"},{"Blue: next / Green: used / Yellow: last","つながる道を選び、店へ戻る道順を作ろう"},{"Scroll: zoom / Middle-drag: pan","ホイール：拡大縮小 / 中ボタンのドラッグ：移動"},
        {"STOP","事故・停止"},{"Score","得点"},{"PIZZA DELIVERY","ピザ配達ゲーム"},{"Start new","最初から"},{"fixed","固定費"},{"wages","日給合計"},{"cash","所持金"},{"Next ","次の費用 "},
        {"ONE DAY AT THE SHOP","ピザ屋の一日"},{"During preparation, hire or train your team, set the delivery promise, then start business. Orders bake automatically in the oven. Only ready pizzas can be dispatched. After closing, all couriers return before settlement.","開店前に採用・訓練とお届け目安を決め、営業を始めましょう。注文のピザは自動で焼けます。焼き上がったら配達員と道順を選んで出発。閉店後は全員の帰還を待って精算します。"},
        {"A shorter promise attracts more orders. A longer promise reduces demand. Existing order deadlines do not change when the promise changes.","お届け目安を短くすると注文が増え、長くすると減ります。すでに受けた注文の期限は変わりません。"},
        {"CHOOSE YOUR ROADS","自分で道順を決めよう"},{"Select up to two orders and an available courier. Click connected roads through the destinations and back to the shop. Compare against individual trips. The suggested route is optional. Press Take out or Space at 85-95% baking for a perfect pizza; on-time delivery adds 15. Ignoring baking gives a standard pizza without penalty.","注文を最大2件と待機中の配達員を選び、つながる道をクリックして配達先から店までの道順を作ります。個別配達の参考時間と比べましょう。推奨ルートも利用できます。焼き加減85〜95%で「取り出す」かSpaceを押すと完璧。期限内の配達で+15です。操作しなくても標準品質で自動的に焼き上がります。"},
        {"MONEY AND REPUTATION","お金・評判・体力"},{"On-time deliveries earn tips and reputation. Late deliveries earn less. Materials, fuel, wages and fixed costs reduce cash. Rest couriers to recover stamina.","期限内の配達はチップと評判につながり、遅れると売上が減ります。材料費・燃料費・日給・固定費と事故の修理費がかかります。店で休ませると体力が回復します。設備は開店準備で購入できます。"},
        {"Choose your team, then start business.","スタッフを確認して営業を始めましょう。"},{"Build your team","スタッフをそろえよう"},{"Start a new campaign.","「最初から」で新しい経営を始めます。"},
        {"Buy during preparation only.","購入できるのは開店準備中だけです。"},{"Advertising already booked today.","今日の広告は購入済みです。"},{"Already at maximum.","最大まで強化済みです。"},{"Not enough cash.","所持金が足りません。"},
        {"Select orders and an available courier, then click a road on the map.","焼き上がった注文と待機中の配達員を選び、地図の道をクリックしてください。"},{"Waiting for orders. New orders appear automatically during opening.","注文を待っています。営業中は自動的に注文が入ります。"},
        {"Route ready! Dispatch, or keep editing. The clock is still running.","道順が完成しました。出発するか、さらに編集できます。時間は進み続けています。"},{"Finish your route and return to Store to compare.","配達先を通って店へ戻る道順を完成させると比較できます。"},
        {"Individual vs your route: select orders and a courier.","個別配達との比較：注文と配達員を選んでください。"},{"Suggestion available after selecting orders / courier","注文と配達員を選ぶと推奨ルートが出ます"},{"Couriers are travelling. Select waiting orders and an idle courier.","配達中です。次の注文と待機中の配達員を選べます。"},
        {"Closed. Couriers will finish their trips before the result is shown.","閉店しました。配達中の全員が帰還すると精算します。"},{"Ready for the next day.","次の日の準備に進めます。"},{"Continue to game over.","経営結果へ進んでください。"},
        {"These orders are still cooking.","この注文はまだ調理中です。"},{"These orders have expired.","この注文は期限切れです。"},{"The store is closed.","営業中だけ出発できます。"},{"Select a courier.","配達員を選んでください。"},{"Visit all selected destinations and return to Store.","選んだ配達先をすべて通り、店に戻る道順を作ってください。"},
        {"This courier is training.","この配達員は訓練中です。"},{"This courier is travelling.","この配達員は配達中です。"},{"No waiting orders.","待機中の注文はありません。"},{"Waiting for new orders...","新しい注文を待っています…"},
        {"on time / tip","期限内 / チップ"},{"Fastest arrival (reference)","最短の到着時間（参考）"},{"Delivery promise","お届け目安"},{"DELIVERY PROMISE","お届け目安"},{"Individual reference","個別配達の参考時間"},{"Optional suggestion","推奨ルート（任意）"},{"Use this route","この道順を使う"},{"Same time","同じ時間"},{"Slower by","遅くなる時間"},{"Saves","短縮時間"},
        {"Start business","営業開始"},{"Next day","次の日へ"},{"Choose training","訓練を選ぶ"},{"Cancel training","訓練を取り消す"},{"Take out","取り出す"},{"Buy oven","窯を増設"},{"Upgrade bikes","バイクを強化"},{"Expand shop","店舗を拡張"},
        {"Menu / Esc","メニュー / Esc"},{"New game","最初から"},{"Continue","続きから"},{"New Game","最初から"},{"How to play","遊び方"},{"Show all","一覧で見る"},{"Skip","スキップ"},{"Return to title","タイトルへ"},{"Back to title","タイトルへ"},{"Try again","もう一度"},
        {"GAME OVER","経営終了"},{"DAY COMPLETE","一日の営業終了"},{"BUSINESS CLOSED","経営を続けられません"},{"BANKRUPT - GAME OVER","破産・経営終了"},{"NEXT ROADS FROM","ここから進める道"},{"PREVIOUS DAYS","これまでの営業"},
        {"Perfect bake bonus (included)","完璧ボーナス（売上に含む）"},{"Equipment maintenance (included)","設備維持費（固定費に含む）"},{"Facility purchases (preparation)","設備購入費（準備費に含む）"},{"Preparation hiring/severance","採用・解雇・訓練・設備の準備費"},
        {"Waiting for oven","調理待ち"},{"Today only","当日のみ有効"},{"Team limit","名簿の上限"},{"Perfect","完璧"},{"Standard","標準"},{"Experienced","経験者"},{"Newcomer","新人"},{"Training...","訓練を選ぶ"},
        {"Driving","運転"},{"LocalKnowledge","土地勘"},{"Capacity","積載量"},{"Drive","運転"},{"Know","土地勘"},{"Service","接客"},{"Endurance","体力"},{"Safety","安全性"},{"Load","積載量"},
        {"BeforeOpening","開店準備"},{"Delivering","配達中"},{"Returning","帰還中"},{"Idle","待機"},{"Training","訓練中"},{"Open","営業中"},{"Closed","閉店・帰還待ち"},{"Ended","営業終了"},
        {"PREPARATION","開店準備"},{"SETTLEMENT","精算"},{"SALES","売上"},{"COSTS","費用"},{"PROFIT","損益"},{"OVENS","窯の増設"},{"BIKES","バイクの強化"},{"ADVERTISING","広告"},{"SHOP","店舗の拡張"},
        {"Staff","スタッフ"},{"Applicants","求人"},{"Management","経営"},{"Facilities","設備"},{"Day","営業日"},{"Cash","所持金"},{"Sales","売上"},{"Revenue","売上"},{"Reputation","評判"},{"Orders","注文"},{"Demand","需要"},{"Team","スタッフ"},
        {"Materials","材料費"},{"Fuel","燃料費"},{"Wages","日給合計"},{"Wage","日給"},{"Fixed","固定費"},{"Profit","利益"},{"Preparation","準備費"},{"Repairs","事故修理費"},{"Severance","解雇費"},{"Hire","採用"},{"Fire","解雇"},{"Train","訓練"},
        {"Current","現在地"},{"Destinations visited","訪問済み"},{"Combined route","まとめ配達"},{"Individual route","個別配達"},{"Single delivery","1件の配達"},{"Road","大通り"},{"Backstreet","裏道"},{"Main","大通り"},{"Store","店"},
        {"Cooking","調理中"},{"Ready","焼き上がり"},{"Delivery to","配達先"},{"Baking pizza","調理中のピザ"},{"Ready pizza","焼き上がりのピザ"},{"Delivered","配達完了"},{"Cancelled","キャンセル"},{"Late","遅れ"},{"LATE","遅れ"},{"left","残り"},
        {"Next","次へ"},{"Small","小"},{"Medium","中"},{"Large","大"},{"maintenance","維持費"},{"speed","速度"},{"risk","事故率"},{"oven","窯"},{"staff slot","人の雇用枠"},{"day","日"},{"Today","今日"},{"Total revenue","累計売上"},{"Total sales","累計売上"},{"Best day","最高売上"},{"Reached day","到達した営業日"},{"Tips included","チップ（売上に含む）"},{"Tips","チップ"},
        {"Cancel","戻る"},{"Resume","再開"},{"Quit","終了"},{"Help","遊び方"},{"Previous","前へ"},{"Close","閉じる"},{"Yes","はい"},{"No","いいえ"},{"Strong","得意"},{"XP","経験値"},{"about","約"},{"per order","ごとに注文"},{"min","下限"},{"round trip(s)","往復"},{"Start","出発地"},{"To","行き先"},{"Visit","訪問先"}
    };
    private static readonly KeyValuePair<string,string>[] ordered=phrases.OrderByDescending(p=>p.Key.Length).ToArray();
    private static readonly Regex expression=new Regex(string.Join("|",ordered.Select(p=>"(?<![A-Za-z])"+Regex.Escape(p.Key)+"(?![A-Za-z])")));
    public static string Japanese(string source)
    {
        if(string.IsNullOrEmpty(source))return source;
        // One regex pass prevents replacement output from being translated a second time.
        return expression.Replace(source,m=>phrases[m.Value]);
    }
}
