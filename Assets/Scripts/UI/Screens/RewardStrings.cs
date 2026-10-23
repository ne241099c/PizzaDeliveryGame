/// <summary>Text for the provisional goal/reward screens; final art can reuse these actions.</summary>
public static class RewardStrings
{
    public const string Goals="目標と報酬", Close="閉じる", Missions="今日のミッション", Grade="店のグレード";
    public const string MissionHelp="開店前に、挑戦したい目標を選べます。選ばなくても営業できます。報酬は精算時に受け取ります。";
    public const string Loan="ローンの返済", Repay="この金額を返す", RepayAll="返せる分をすべて", Results="今回の成績";
    public const string Normal="通常モード", Endless="エンドレス", Start="このモードで始める", Cancel="戻る";
    public const string Mode="遊び方を選ぶ", Records="記録・称号・図鑑", Continue="エンドレスで続ける", Title="タイトルへ";
    public const string NormalHelp="20日でローン完済を目指します。最後に成績とランクを確認できます。";
    public const string EndlessHelp="日数の制限なく経営します。自由な目標を決め、最高記録を伸ばしましょう。";
    public const string EndlessGoals="エンドレスの自由な目標", AssetsGoal="純資産を増やす", ReputationGoal="評判を高める", StreakGoal="黒字を続ける";
    public const string RepayHelp="返済は任意です。返したお金は設備・訓練には使えません。翌日の費用も残しましょう。";
    public static string Hud(decimal loan,int selected)=>$"ローン {UiKit.Yen(loan)}・目標 {selected}件";
    public static string Repayment(decimal amount)=>$"返済額 {UiKit.Yen(amount)}";
}
