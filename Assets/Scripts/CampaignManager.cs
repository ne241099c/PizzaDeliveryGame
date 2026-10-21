using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Campaign state survives daily resets; business rules stay in the existing clock/flow.</summary>
[DefaultExecutionOrder(-75)]
public sealed class CampaignManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int bankruptcyGraceDays = 0;
    [SerializeField] private RecruitmentManager recruitment;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private GameClock clock;
    [SerializeField] private OrderListUI orders;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DeliveryDynamics dynamics;
    [SerializeField] private OrderSpawner spawner;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private ScoreManager score;
    [SerializeField] private DayStats stats;
    [SerializeField] private DeliveryPlannerUI planner;
    [SerializeField] private GameObject preparationPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private TextMeshProUGUI dayText;
    public int DayNumber { get; private set; } = 1;
    public int TotalDelivered { get; private set; }
    public int TotalScore { get; private set; }
    private void Start() { openButton.onClick.AddListener(StartBusiness); PrepareDay(); }
    private void OnEnable() => clock.OnDayEnded += SettleDay;
    private void OnDisable() => clock.OnDayEnded -= SettleDay;
    private void Update() => dayText.text=$"Day {DayNumber} / {clock.Phase}";
    public bool IsBankrupt { get; private set; }
    public int ConsecutiveNegativeDays { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal BestDailyRevenue { get; private set; }
    public bool ShouldDeclareBankruptcy(decimal cash,int negativeDays) => cash<0 && negativeDays>bankruptcyGraceDays;
    private void SettleDay()
    {
        economy.SettleDay(); TotalDelivered+=stats.DeliveredCount; TotalScore+=stats.TotalScore;
        TotalRevenue+=economy.DailyRevenue;BestDailyRevenue=System.Math.Max(BestDailyRevenue,economy.DailyRevenue);
        ConsecutiveNegativeDays=economy.Cash<0 ? ConsecutiveNegativeDays+1 : 0;
        IsBankrupt=ShouldDeclareBankruptcy(economy.Cash,ConsecutiveNegativeDays);
    }
    public string FinancialSummary => $"Day {DayNumber} / {(IsBankrupt ? "BANKRUPT - GAME OVER" : "SETTLEMENT")}\n"
        +$"Revenue: {economy.Ledger.Revenue:F2}\nMaterials: -{economy.Ledger.Materials:F2}\nFuel: -{economy.Ledger.Fuel:F2}\nWages: -{economy.Ledger.Wages:F2}\nFixed: -{economy.Ledger.FixedCosts:F2}\nProfit: {economy.Ledger.Profit:+0.00;-0.00;0.00}\n"
        +$"Preparation hiring/severance: -{economy.Ledger.PreparationCosts:F2}\nCash: {economy.OpeningCash:F2} -> {economy.Cash:F2}\n"
        +$"Total revenue: {TotalRevenue:F2} / Best day: {BestDailyRevenue:F2}\n";
    public void StartBusiness()
    {
        if(clock.Phase!=DayPhase.BeforeOpening || IsBankrupt)return;
        preparationPanel.SetActive(false); clock.BeginDay();
    }
    public void NextDay()
    {
        if(clock.Phase!=DayPhase.Ended || IsBankrupt)return;
        DayNumber++; PrepareDay();
    }
    public void NewGame()
    {
        flow.ResetForNewGame();
        IsBankrupt=false;ConsecutiveNegativeDays=0;TotalRevenue=BestDailyRevenue=0;
        recruitment.ResetRandom();spawner.ResetRandom();roster.ResetToInitialRoster();economy.NewGame(); DayNumber=1; TotalDelivered=TotalScore=0; reputation.ResetForDay(); spawner.ResetPolicy(); PrepareDay();
    }
    private void PrepareDay()
    {
        economy.PrepareDay(); clock.PrepareDay(); orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        orders.ClearSelection(); roster.ResetDailyState(); dynamics.ResetForDay(); spawner.ResetForDay();
        score.ResetForDay(); stats.ResetForDay(); planner.PrepareDay(); recruitment.GenerateDay();
        preparationPanel.SetActive(true);
    }
}
