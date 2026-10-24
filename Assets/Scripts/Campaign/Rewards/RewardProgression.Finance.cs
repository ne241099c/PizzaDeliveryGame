using System;
using System.Linq;
using UnityEngine;

public sealed partial class RewardProgression
{
    [Header("仮バランス値：R-7測定後にユーザーが調整")]
    [SerializeField,Min(0)] private float initialLoan=4000;
    [SerializeField,Range(0,1)] private float dailyInterest=.005f;
    [SerializeField,Min(0)] private float payoffBonus=100;
    [SerializeField,Min(1)] private int recentDays=5;
    [SerializeField] private RewardGradeDefinition[] grades={
        new(){name="屋台",districts=8,menuKinds=1,equipmentLevel=2},
        new(){name="街の人気店",reputation=40,revenue=4000,onTimeRate=.8f,districts=10,menuKinds=2,equipmentLevel=4},
        new(){name="行列のできる店",reputation=70,revenue=12000,threeStarRate=.3f,districts=12,menuKinds=3,equipmentLevel=7},
        new(){name="名店",reputation=90,netAssets=4000,districts=14,menuKinds=3,equipmentLevel=10}
    };
    private string promotionNotice="";
    public decimal Loan=>CampaignSaveManager.Decimal(State.loan);
    public decimal PayoffBonus=>BusinessEconomy.Money((decimal)payoffBonus);
    public decimal InterestFor(decimal amount)=>BusinessEconomy.Money(amount*(decimal)dailyInterest);
    private RewardGradeDefinition CurrentGrade=>grades[Mathf.Clamp(State.grade-1,0,grades.Length-1)];
    public string GradeName=>CurrentGrade.name;
    public int UnlockedDistricts=>CurrentGrade.districts;
    public int UnlockedMenuKinds=>CurrentGrade.menuKinds;
    public int UnlockedEquipmentLevel=>CurrentGrade.equipmentLevel;
    public string NextGradeProgress
    {
        get
        {
            if(State.grade>=grades.Length)return "すべての店グレードに到達しました。";
            var g=grades[State.grade];
            return $"次：{g.name}\n評判 {FindFirstObjectByType<ReputationManager>().Value:F0}/{g.reputation:F0}　累計売上 {Campaign.TotalRevenue:F0}/{g.revenue:F0}\n直近期限内率 {RecentOnTime:P0}/{g.onTimeRate:P0}　★3率 {ThreeStarRate:P0}/{g.threeStarRate:P0}\n純資産 {NetAssets:F0}/{g.netAssets:F0}";
        }
    }
    public float RecentOnTime{get{int n=State.recent.Sum(m=>m.delivered+m.cancelled);return n>0?(float)State.recent.Sum(m=>m.delivered-m.late)/n:0;}}
    public float ThreeStarRate=>State.totalDelivered>0?(float)State.totalThreeStars/State.totalDelivered:0;
    public bool Qualifies(RewardGradeDefinition g,float reputation,decimal revenue,decimal assets,float onTime,float stars)=>reputation>=g.reputation && revenue>=(decimal)g.revenue && (g.netAssets<=0 || assets>=(decimal)g.netAssets) && onTime>=g.onTimeRate && stars>=g.threeStarRate;
    private void Promote()
    {
        int before=State.grade;
        while(State.grade<grades.Length && Qualifies(grades[State.grade],FindFirstObjectByType<ReputationManager>().Value,Campaign.TotalRevenue,NetAssets,RecentOnTime,ThreeStarRate))State.grade++;
        promotionNotice=before<State.grade?$"{GradeName}に昇格！ 街区{UnlockedDistricts}・ピザ{UnlockedMenuKinds}種・設備Lv{UnlockedEquipmentLevel}を解放":"";
    }
    public bool CanRepay=>Clock.Phase==DayPhase.Ended && !Campaign.IsBankrupt && Loan>0 && Economy.Cash>0;
    public bool Repay(decimal amount)
    {
        amount=BusinessEconomy.Money(amount);
        if(!CanRepay || amount<=0 || amount>Loan || amount>Economy.Cash)return false;
        Economy.ApplyGoalCash(-amount);State.loan=Money(Loan-amount);
        if(Loan==0 && !State.loanCelebrated){State.loanCelebrated=true;Economy.ApplyGoalCash((decimal)payoffBonus);EvaluateProfileAchievements();}
        Promote();CheckCustomGoal();SaveCheckpoint();return true;
    }
}
