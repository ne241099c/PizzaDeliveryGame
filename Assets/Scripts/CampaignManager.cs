using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Campaign state survives daily resets; business rules stay in the existing clock/flow.</summary>
[DefaultExecutionOrder(-75)]
public sealed class CampaignManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int bankruptcyGraceDays = 0;
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private CampaignSaveManager saveGame;
    public event System.Action PreparationEntered;
    [SerializeField] private TrainingManager training;
    [SerializeField] private CourierProgression progression;
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
    [SerializeField] private KitchenCrew crew;
    [SerializeField] private CookingManager cooking;
    [SerializeField] private HandoverManager handover;
    [SerializeField] private GameObject preparationPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private TextMeshProUGUI dayText;
    public int DayNumber { get; private set; } = 1;
    public int TotalDelivered { get; private set; }
    public int TotalScore { get; private set; }
    private void Start() {openButton.onClick.AddListener(StartBusiness);if(saveGame!=null)saveGame.Startup();else PrepareDay();}
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
        saveGame?.RecordDay(DayNumber,economy.DailyRevenue,TotalRevenue,TotalDelivered);
        ConsecutiveNegativeDays=economy.Cash<0 ? ConsecutiveNegativeDays+1 : 0;
        IsBankrupt=ShouldDeclareBankruptcy(economy.Cash,ConsecutiveNegativeDays);
        if(IsBankrupt){saveGame?.DeleteSave();facilities?.NewGame();}
    }
    public string FinancialSummary => $"Day {DayNumber} / {(IsBankrupt ? "BANKRUPT - GAME OVER" : "SETTLEMENT")}\n"
        +$"Revenue: {economy.Ledger.Revenue:F2}\nQuality bonus (included): {economy.Ledger.QualityBonus:F2}\nMaterials: -{economy.Ledger.Materials:F2}\nFuel: -{economy.Ledger.Fuel:F2}\nWages: -{economy.Ledger.Wages:F2}\nRepairs: -{economy.Ledger.Repairs:F2}\nFixed: -{economy.Ledger.FixedCosts:F2}\nEquipment maintenance (included): {economy.Ledger.EquipmentMaintenance:F2}\nFacility purchases (preparation): {economy.Ledger.FacilityPurchases:F2}\nProfit: {economy.Ledger.Profit:+0.00;-0.00;0.00}\n"
        +$"Preparation hiring/severance: -{economy.Ledger.PreparationCosts:F2}\nCash: {economy.OpeningCash:F2} -> {economy.Cash:F2}\n"
        +$"Total revenue: {TotalRevenue:F2} / Best day: {BestDailyRevenue:F2}\n"+progression.DailySummary+"\n";
    public void StartBusiness()
    {
        if(clock.Phase!=DayPhase.BeforeOpening || IsBankrupt || (saveGame!=null && saveGame.AwaitingChoice && saveGame.AutomaticSavingEnabled))return;
        facilities?.BeginBusiness();saveGame?.SavePreparation();UIVisibility.Set(preparationPanel,false); clock.BeginDay();
    }
    public void NextDay()
    {
        if(clock.Phase!=DayPhase.Ended || IsBankrupt)return;
        DayNumber++; PrepareDay();
    }
    public void NewGame()
    {
        flow.ResetForNewGame();FindFirstObjectByType<MapManager>().NewCampaignCity();facilities?.NewGame();spawner.RefreshMinimumPromise();
        IsBankrupt=false;ConsecutiveNegativeDays=0;TotalRevenue=BestDailyRevenue=0;
        recruitment.ResetRandom();spawner.ResetRandom();if(cooking!=null)cooking.ResetRandom();if(crew!=null)crew.ResetToInitial();roster.ResetToInitialRoster();economy.NewGame(); DayNumber=1; TotalDelivered=TotalScore=0; reputation.ResetForDay(); spawner.ResetPolicy(); PrepareDay();
    }
    public void RestorePreparation(CampaignSaveData data)
    {
        flow.ResetForNewGame();DayNumber=data.day;TotalDelivered=data.totalDelivered;TotalScore=data.totalScore;
        TotalRevenue=CampaignSaveManager.Decimal(data.totalRevenue);BestDailyRevenue=CampaignSaveManager.Decimal(data.bestDailyRevenue);
        ConsecutiveNegativeDays=data.negativeDays;IsBankrupt=false;clock.PrepareDay();
        facilities?.Restore(data.facilities);
        orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));FindFirstObjectByType<MapManager>().RestoreCity(data.city);spawner.RefreshMinimumPromise();
        orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));orders.ClearSelection();roster.RestoreRoster(data.people);if(crew!=null)crew.Restore(data.crew);
        reputation.Restore(data.reputation);dynamics.ResetForDay();spawner.ResetForDay();spawner.SetPromiseSeconds(data.promise);
        economy.RestorePreparation(CampaignSaveManager.Decimal(data.cash),CampaignSaveManager.Decimal(data.openingCash),CampaignSaveManager.Decimal(data.preparationCosts),DayNumber);
        economy.Ledger.FacilityPurchases=CampaignSaveManager.Decimal(data.facilityPurchaseCosts);
        score.ResetForDay();stats.ResetForDay();training.RestoreAssignments(data.training);
        recruitment.RestoreApplicants(data.applicants.ConvertAll(a=>new Applicant(a.person,a.baselines,a.strengths,a.weaknesses)));
        recruitment.RestoreKitchenApplicants(data.kitchenApplicants.ConvertAll(k=>new KitchenApplicant(k)));handover?.Clear();
        progression.PrepareDay();planner.PrepareDay();UIVisibility.Set(preparationPanel,true);PreparationEntered?.Invoke();
    }
    private void PrepareDay()
    {
        facilities?.PrepareDay();economy.PrepareDay(DayNumber); clock.PrepareDay(); orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        orders.ClearSelection(); roster.ResetDailyState(); crew?.ResetDailyState(); handover?.Clear(); dynamics.ResetForDay(); spawner.ResetForDay();
        FindFirstObjectByType<MapManager>().PrepareCity(DayNumber);
        training.PrepareDay();score.ResetForDay(); stats.ResetForDay(); planner.PrepareDay(); recruitment.GenerateDay();progression.PrepareDay();
        UIVisibility.Set(preparationPanel,true);PreparationEntered?.Invoke();
    }
}
