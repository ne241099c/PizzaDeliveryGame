/// <summary>Text for the provisional goal/reward screens; final art can reuse these actions.</summary>
public static class RewardStrings
{
    public const string Provisional="";
    public const string Goals="ミッションとローン", Close="閉じる", Missions="今日のミッション", Grade="店のグレード";
    public const string MissionHelp="今日のミッションは全部が対象です。達成すると、精算のときに報酬を自動で受け取ります。";
    public const string MenuMissions="ミッション・ローン",SettlementLauncher="ローン返済・ミッション";
    public const string Loan="ローンの返済", Repay="この金額を返す", RepayAll="返せる分をすべて", Results="今回の成績";
    public const string Normal="通常モード", Endless="エンドレス", Start="このモードで始める", Cancel="戻る";
    public const string Mode="遊び方を選ぶ", Records="記録・トロフィー", Continue="エンドレスで続ける", Title="タイトルへ";
    public const string NormalHelp="20日でローン完済を目指します。\n最終日に成績を評価します。";
    public const string EndlessHelp="日数制限なく経営できます。\n自由な目標と最高記録に挑戦。";
    public const string EndlessGoals="エンドレスの自由な目標", AssetsGoal="純資産を増やす", ReputationGoal="評判を高める", StreakGoal="遅れなしを続ける";
    public const string RepayHelp="返済は任意です。返したお金は設備・訓練には使えません。翌日の費用も残しましょう。";

    public static string Repayment(decimal amount)=>$"返済額 ¥{amount:#,0.00}";
}
