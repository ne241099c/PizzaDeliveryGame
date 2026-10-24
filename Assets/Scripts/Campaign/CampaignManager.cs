using UnityEngine;

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
    [SerializeField] private OrderBook orders;
    [SerializeField] private CourierRoster roster;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DeliveryDynamics dynamics;
    [SerializeField] private OrderSpawner spawner;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private ScoreManager score;
    [SerializeField] private DayStats stats;
    [SerializeField] private DeliveryPlanner planner;
    [SerializeField] private KitchenCrew crew;
    [SerializeField] private CookingManager cooking;
    [SerializeField] private HandoverManager handover;
    public RewardProgression Rewards=>RewardProgression.Get(this);
    private void Awake()=>RewardProgression.Get(this);
    public void DeclareBankruptcy()=>IsBankrupt=true;
    public int DayNumber { get; private set; } = 1;
    public int TotalDelivered { get; private set; }
    public int TotalScore { get; private set; }
    // The campaign starts once the map is ready: a new game generates its town and a save restores one, and both need the map.
    private void Start()
    {
        var map=FindFirstObjectByType<MapManager>();
        if(map!=null && !map.IsReady){map.MapReady+=StartWhenMapReady;return;}
        Begin();
    }
    private void StartWhenMapReady(){FindFirstObjectByType<MapManager>().MapReady-=StartWhenMapReady;Begin();}
    private void Begin(){if(saveGame!=null)saveGame.Startup();else PrepareDay();}
    private void OnEnable() => clock.OnDayEnded += SettleDay;
    private void OnDisable() => clock.OnDayEnded -= SettleDay;
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
        Rewards.SettleDay();
        ConsecutiveNegativeDays=economy.Cash<0 ? ConsecutiveNegativeDays+1 : 0;
        IsBankrupt=IsBankrupt || economy.Cash<0;
        // A bankrupt game ends here: SavePreparation removes its save and the equipment returns to the start.
        if(IsBankrupt)facilities?.NewGame();
        saveGame?.SavePreparation();
    }
    public string FinancialSummary => $"Day {DayNumber} / {(IsBankrupt ? "BANKRUPT - GAME OVER" : "SETTLEMENT")}\n"
        +$"Revenue: {economy.Ledger.Revenue:F2}\nQuality bonus (included): {economy.Ledger.QualityBonus:F2}\nMaterials: -{economy.Ledger.Materials:F2}\nWaste ({economy.Ledger.WastedPizzas}): -{economy.Ledger.Waste:F2}\nFuel: -{economy.Ledger.Fuel:F2}\nWages: -{economy.Ledger.Wages:F2}\nRepairs: -{economy.Ledger.Repairs:F2}\nFixed: -{economy.Ledger.FixedCosts:F2}\nEquipment maintenance (included): {economy.Ledger.EquipmentMaintenance:F2}\nFacility purchases (preparation): {economy.Ledger.FacilityPurchases:F2}\nProfit: {economy.Ledger.Profit:+0.00;-0.00;0.00}\n"
        +$"Preparation hiring/severance: -{economy.Ledger.PreparationCosts:F2}\nCash: {economy.OpeningCash:F2} -> {economy.Cash:F2}\n"
        +$"Total revenue: {TotalRevenue:F2} / Best day: {BestDailyRevenue:F2}\n"+progression.DailySummary+"\n";
    public void StartBusiness()
    {
        if(clock.Phase!=DayPhase.BeforeOpening || IsBankrupt)return;
        facilities?.BeginBusiness();saveGame?.SavePreparation();clock.BeginDay();
    }
    public void NextDay()
    {
        if(clock.Phase!=DayPhase.Ended || IsBankrupt || Rewards.HasPendingResult)return;
        DayNumber++; PrepareDay();
    }
    public void NewGame()
    {
        Rewards.NewGame();flow.ResetForNewGame();FindFirstObjectByType<MapManager>().NewCampaignCity();facilities?.NewGame();spawner.RefreshMinimumPromise();
        IsBankrupt=false;ConsecutiveNegativeDays=0;TotalRevenue=BestDailyRevenue=0;
        recruitment.ResetRandom();
        spawner.ResetRandom();
        if(cooking!=null)cooking.ResetRandom();
        if(crew!=null)crew.ResetToInitial();
        roster.ResetToInitialRoster();
        economy.NewGame();
        DayNumber=1;
        TotalDelivered=TotalScore=0;
        reputation.ResetForDay();
        spawner.ResetPolicy();
        PrepareDay();
    }
    public void RestorePreparation(CampaignSaveData data)
    {
        Rewards.Restore(data.rewards,data.day);flow.ResetForNewGame();DayNumber=data.day;TotalDelivered=data.totalDelivered;TotalScore=data.totalScore;
        TotalRevenue=CampaignSaveManager.Decimal(data.totalRevenue);BestDailyRevenue=CampaignSaveManager.Decimal(data.bestDailyRevenue);
        ConsecutiveNegativeDays=data.negativeDays;IsBankrupt=false;clock.PrepareDay();
        facilities?.Restore(data.facilities);
        orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        FindFirstObjectByType<MapManager>().RestoreCity(data.city);
        spawner.RefreshMinimumPromise();
        orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        orders.ClearSelection();
        roster.RestoreRoster(data.people);
        if(crew!=null)crew.Restore(data.crew);
        reputation.Restore(data.reputation);dynamics.ResetForDay();spawner.ResetForDay();spawner.SetPromiseSeconds(data.promise);
        economy.RestorePreparation(CampaignSaveManager.Decimal(data.cash),CampaignSaveManager.Decimal(data.openingCash),CampaignSaveManager.Decimal(data.preparationCosts),DayNumber);
        economy.Ledger.FacilityPurchases=CampaignSaveManager.Decimal(data.facilityPurchaseCosts);
        score.ResetForDay();stats.ResetForDay();training.RestoreAssignments(data.training);training.RestoreKitchenAssignments(data.kitchenTraining);
        recruitment.RestoreApplicants(data.applicants.ConvertAll(a=>new Applicant(a.person,a.baselines,a.strengths,a.weaknesses)));
        recruitment.RestoreKitchenApplicants(data.kitchenApplicants.ConvertAll(k=>new KitchenApplicant(k)));handover?.Clear();
        progression.PrepareDay();planner.PrepareDay();
        if(Rewards.State.ended){clock.RestoreEnded();economy.RestoreSettledLedger(Rewards.State.ledger,Rewards.State.last.wasted);stats.RestoreRewardMetrics(Rewards.State.last);cooking.RestoreProduction(Rewards.State.last);IsBankrupt=economy.Cash<0;}
        PreparationEntered?.Invoke();
    }
    private void PrepareDay()
    {
        facilities?.PrepareDay();
        economy.PrepareDay(DayNumber);
        clock.PrepareDay();
        orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        orders.ClearSelection(); roster.ResetDailyState(); crew?.ResetDailyState(); handover?.Clear(); dynamics.ResetForDay(); spawner.ResetForDay();
        FindFirstObjectByType<MapManager>().PrepareCity(DayNumber);
        training.PrepareDay();score.ResetForDay(); stats.ResetForDay(); planner.PrepareDay(); recruitment.GenerateDay();progression.PrepareDay();
        Rewards.PrepareDay();PreparationEntered?.Invoke();
    }
}
