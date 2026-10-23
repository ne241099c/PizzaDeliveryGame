/// <summary>The "how to play" pages, shown from the title and from the in-game menu. The first line of each page is its title.</summary>
public static class HelpPages
{
    public static readonly string[] Pages={
        "営業の流れ\n開店準備でスタッフ・設備・お届け目安を決めて「営業開始」。営業は3分です。左上のスイッチ（または 1 / 2、Q / E）で厨房と配達指示を切り替えます。閉店後、全員が帰ったら精算です。",
        "厨房\n内勤が自動で調理します。自分で作るときは、伝票 → 生地 → ソース → チーズ → 指定の具 → 窯の順。緑の帯で窯から出すと高品質です。焼けたピザは自動で箱に入り、保温棚に並びます。",
        "配達指示\n① 焼き上がった伝票を、下の配達員のレーンへドラッグします（地図のピンからも可）。同じレーンに入れると、まとめて運びます。\n② 赤いルートを、通りたい道までドラッグして直し、「出発」。届ける順番は、レーンの伝票を入れ替えます。",
        "目安・遅れ・売上\n目安を過ぎても受付猶予中は出発でき、減点で配達できます。遅れるとチップ・品質ボーナスは付きません。出発しないまま猶予を過ぎるか閉店すると、キャンセルです。目安を短くすると注文が増えます。",
        "セーブ\n開店準備のたびに自動でセーブします。営業の途中でやめたときは、「続きから」でその日の開店準備から再開します。最高記録は、ゲームをやり直しても残ります。"};
    public static string Title(int page)=>Pages[page].Split('\n')[0];
    public static string Body(int page){var parts=Pages[page].Split(new[]{'\n'},2);return parts.Length>1?parts[1]:"";}
}
