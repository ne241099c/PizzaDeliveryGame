using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public sealed class SettlementScreenUI : MonoBehaviour
{
    [SerializeField] private ScreenRouter router;
    [SerializeField] private GameClock clock;
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private DayStats stats;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private ScoreManager score;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private CourierProgression progression;
    [SerializeField] private DeliveryPlannerUI planner;
    [SerializeField] private TextMeshProUGUI pageText,gameOverText;
    [SerializeField] private Button advance,skip,all,replay,gameOver,retry,title;
    [SerializeField,Min(0)] private float pageSeconds=1;
    private readonly List<DeliveryTrip> trips=new();
    private float timer,openingReputation;
    public int Page {get;private set;}
    public bool IsShowingAll {get;private set;}
    private void Start()
    {
        clock.OnDayStarted+=Opening;clock.OnDayEnded+=Settlement;flow.Dispatched+=Trip;
        advance.onClick.AddListener(AdvancePage);skip.onClick.AddListener(()=>{Page=4;IsShowingAll=false;Refresh();});all.onClick.AddListener(ShowAll);
        gameOver.onClick.AddListener(()=>{RefreshGameOver();router.Show(ScreenId.GameOver);});
        retry.onClick.AddListener(()=>FindFirstObjectByType<CampaignSaveManager>().StartNewGame());title.onClick.AddListener(()=>router.Show(ScreenId.Title));
    }
    private void OnDestroy(){clock.OnDayStarted-=Opening;clock.OnDayEnded-=Settlement;flow.Dispatched-=Trip;}
    private void Opening(){trips.Clear();openingReputation=reputation.Value;}
    private void Trip(DeliveryTrip trip)=>trips.Add(trip);
    private void Settlement(){Page=0;IsShowingAll=false;timer=0;Refresh();RefreshGameOver();}
    public void AdvancePage(){Page=Mathf.Min(4,Page+1);timer=0;IsShowingAll=false;Refresh();}
    public void ShowAll(){IsShowingAll=true;Page=4;Refresh();}
    private void Update()
    {
        if(router.Current!=ScreenId.Settlement || clock.Phase!=DayPhase.Ended)return;
        if(!IsShowingAll && Page<4 && pageSeconds>0){timer+=Time.unscaledDeltaTime;if(timer>=pageSeconds)AdvancePage();}
    }
    public void Refresh()
    {
        var l=economy.Ledger;
        int tips=trips.Sum(t=>t.Deliveries.Where(d=>d.Resolved).Sum(d=>score.TipForArrival(d.ArrivesAt,d.Order.Deadline,t.ServiceAtDispatch)));
        string[] pages={
            $"SALES\nRevenue {l.Revenue:F2}\nDelivered {stats.DeliveredCount} / Late {stats.LateCount}\nTips included: {tips}\nPerfect bake bonus (included): {l.PerfectBakeBonus:F2}",
            $"COSTS\nMaterials {l.Materials:F2}\nFuel {l.Fuel:F2}\nWages {l.Wages:F2}\nFixed {l.FixedCosts:F2}\nEquipment maintenance (included) {l.EquipmentMaintenance:F2}\nFacility purchases (preparation) {l.FacilityPurchases:F2}\nPreparation {l.PreparationCosts:F2}",
            $"PROFIT\n{l.Profit:+0.00;-0.00;0.00}\nCash {l.OpeningCash:F2} -> {economy.Cash:F2}",
            progression.DailySummary+$"\nReputation {openingReputation:F1} -> {reputation.Value:F1}",
            campaign.IsBankrupt?"BUSINESS CLOSED\nContinue to game over.":"DAY COMPLETE\nReady for the next day."};
        pageText.text=IsShowingAll?campaign.FinancialSummary+planner.ResultSummary:pages[Page];
        UIVisibility.Set(advance.gameObject,Page<4);UIVisibility.Set(replay.gameObject,Page==4 && !campaign.IsBankrupt);UIVisibility.Set(gameOver.gameObject,Page==4 && campaign.IsBankrupt);
    }
    private void RefreshGameOver()
    {
        gameOverText.text=$"GAME OVER\nReached day {campaign.DayNumber}\nTotal revenue {campaign.TotalRevenue:F2} / Best day {campaign.BestDailyRevenue:F2}\n\n"
            +string.Join("\n",roster.People.Select(p=>$"{p.Name} / Drive {p.Driving:F2} / Know {p.LocalKnowledge:F2} / Service {p.Service:F2} / Endurance {p.Endurance:F2} / Safety {p.Safety:F2} / Load {p.Capacity}"));
    }
}
