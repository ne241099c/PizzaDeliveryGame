using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Campaign state survives daily resets; business rules stay in the existing clock/flow.</summary>
[DefaultExecutionOrder(-75)]
public sealed class CampaignManager : MonoBehaviour
{
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
    private void SettleDay() { TotalDelivered+=stats.DeliveredCount; TotalScore+=stats.TotalScore; }
    public void StartBusiness()
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return;
        preparationPanel.SetActive(false); clock.BeginDay();
    }
    public void NextDay()
    {
        if(clock.Phase!=DayPhase.Ended)return;
        DayNumber++; PrepareDay();
    }
    public void NewGame()
    {
        if(flow.ActiveTrips.Count>0) { UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex); return; }
        economy.NewGame(); DayNumber=1; TotalDelivered=TotalScore=0; reputation.ResetForDay(); spawner.ResetPolicy(); PrepareDay();
    }
    private void PrepareDay()
    {
        economy.PrepareDay(); clock.PrepareDay(); orders.RemoveOrders(new System.Collections.Generic.List<Order>(orders.Orders));
        orders.ClearSelection(); roster.ResetDailyState(); dynamics.ResetForDay(); spawner.ResetForDay();
        score.ResetForDay(); stats.ResetForDay(); planner.PrepareDay();
        preparationPanel.SetActive(true);
    }
}
