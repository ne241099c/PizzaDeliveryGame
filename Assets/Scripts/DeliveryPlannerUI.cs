using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls scene-authored UI. Only data rows are created at runtime.</summary>
[DefaultExecutionOrder(-200)]
public class DeliveryPlannerUI : MonoBehaviour
{
    [SerializeField] private OrderListUI orders;
    [SerializeField] private DeliveryPersonSelector people;
    [SerializeField] private MapManager map;
    [SerializeField] private GameClock clock;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DayStats stats;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI routeSummary;
    [SerializeField] private TextMeshProUGUI comparisonText;
    [SerializeField] private TextMeshProUGUI routePath;
    [SerializeField] private TextMeshProUGUI nextRoadsTitle;
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private RectTransform roadContent;
    [SerializeField] private Button roadOptionPrefab;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button suggestButton;
    [SerializeField] private Button dispatchButton;
    [SerializeField] private Button dispatchSuggestionButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button fitButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField, Min(0.1f)] private float activitySeconds = 5f;
    private ManualDeliveryRoute route;
    private RouteSuggestion suggestion;
    private DeliveryPlan dispatchedPlan;
    private float? dispatchedReference;
    private float activityUntil;
    private readonly List<Button> roadButtons = new();
    private bool subscribed;
    public DeliveryPlan SelectedPlan { get; private set; }
    public ManualDeliveryRoute Route => route;
    public float? IndividualReferenceTime { get; private set; }

    private static readonly Color AccentColor = new(0.32f, 0.88f, 0.69f, 1f);

    private void Start()
    {
        orders.SelectionChanged += OnOrdersChanged;
        orders.OrdersChanged += RefreshRoute;
        people.SelectionChanged += OnPersonChanged;
        map.MapReady += OnMapReady;
        map.EdgeClicked += AppendRoad;
        flow.Message += ShowActivity;
        clock.OnClosed += OnClosed;
        clock.OnDayEnded += ShowResults;
        undoButton.onClick.AddListener(UndoRoad);
        clearButton.onClick.AddListener(ClearRoute);
        suggestButton.onClick.AddListener(ApplySuggestion);
        dispatchButton.onClick.AddListener(Dispatch);
        dispatchSuggestionButton.onClick.AddListener(DispatchWithSuggestion);
        fitButton.onClick.AddListener(FitMapCamera);
        resetButton.onClick.AddListener(Restart);
        replayButton.onClick.AddListener(Restart);
        roadButtons.AddRange(roadContent.GetComponentsInChildren<Button>(true));
        resultsPanel.SetActive(false);
        subscribed = true;
        if (map.IsReady) OnMapReady();
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        orders.SelectionChanged -= OnOrdersChanged;
        orders.OrdersChanged -= RefreshRoute;
        people.SelectionChanged -= OnPersonChanged;
        map.MapReady -= OnMapReady;
        map.EdgeClicked -= AppendRoad;
        flow.Message -= ShowActivity;
        clock.OnClosed -= OnClosed;
        clock.OnDayEnded -= ShowResults;
    }

    private void OnMapReady()
    {
        route = new ManualDeliveryRoute(map);
        FitMapCamera();
        UpdateSuggestion();
        RefreshRoute();
    }

    private void OnOrdersChanged()
    {
        if (orders.SelectedOrders.Count > 0)
        {
            dispatchedPlan = null;
            dispatchedReference = null;
        }
        route?.Clear();
        UpdateSuggestion();
        RefreshRoute();
    }

    private void OnPersonChanged()
    {
        // Keep the drawn path, and recalculate its time using the new courier's skill.
        UpdateSuggestion();
        RefreshRoute();
    }

    private void UpdateSuggestion()
    {
        suggestion = RoadPathfinder.Suggest(map, orders.SelectedOrders, people.SelectedPerson);
        IndividualReferenceTime = RoadPathfinder.GetIndividualReferenceTime(map, orders.SelectedOrders, people.SelectedPerson);
    }

    private void Update()
    {
        int seconds = Mathf.CeilToInt(clock.RemainingSeconds);
        string phase = clock.Phase == DayPhase.Open ? "OPEN" : clock.Phase == DayPhase.Closed ? "CLOSED"
            : clock.Phase == DayPhase.Ended ? "DONE" : "READY";
        clockText.text = $"{phase} {seconds / 60:00}:{seconds % 60:00}";
        dispatchButton.interactable = flow.CanDispatch(SelectedPlan, people.SelectedPerson);
        dispatchSuggestionButton.interactable = clock.Phase == DayPhase.Open && suggestion != null && people.SelectedPerson != null && people.SelectedPerson.IsAvailable;
        clock.IsPlanning = clock.Phase == DayPhase.Open && orders.SelectedOrders.Count > 0 && people.SelectedPerson != null;
        if (clock.ElapsedSeconds > activityUntil) activityText.text = "";
    }

    private void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    private void ShowActivity(string message) { activityText.text = message; activityUntil = clock.ElapsedSeconds + activitySeconds; }
    private void OnClosed() { route?.Clear(); RefreshRoute(); }
    private void ShowResults()
    {
        resultText.text = $"DAY COMPLETE\n\nScore: {stats.TotalScore}\nDelivered: {stats.DeliveredCount}\nLate: {stats.LateCount}\nCancelled: {stats.CancelledCount}";
        resultsPanel.SetActive(true);
    }

    public void Dispatch()
    {
        DeliveryPlan plan = SelectedPlan;
        float? baseline = IndividualReferenceTime;
        if (!flow.Dispatch(plan, people.SelectedPerson)) return;
        dispatchedPlan = plan;
        dispatchedReference = baseline;
        UpdateComparison(plan, baseline, plan.Orders.Count);
        statusText.text = $"Dispatched / estimated return in {clock.ToRealSeconds(plan.TotalTime):F1}s. Select another order and idle courier.";
    }

    public void DispatchWithSuggestion()
    {
        if (clock.Phase != DayPhase.Open || suggestion == null) return;
        ApplySuggestion();
        Dispatch();
    }

    public void AppendRoad(string edgeId)
    {
        if (route == null || clock.Phase != DayPhase.Open || orders.SelectedOrders.Count == 0 || people.SelectedPerson == null)
        {
            statusText.text = "Select orders and a courier first.";
            return;
        }
        if (!route.TryAppend(edgeId, out string message))
        {
            statusText.text = message;
            return;
        }
        RefreshRoute();
    }

    public void FitMapCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || map == null || !map.IsReady || map.Nodes.Count == 0) return;
        float minX = map.Nodes.Min(n => n.Position.x);
        float maxX = map.Nodes.Max(n => n.Position.x);
        float minY = map.Nodes.Min(n => n.Position.y);
        float maxY = map.Nodes.Max(n => n.Position.y);
        camera.transform.position = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f - 0.3f, -10);
        camera.orthographicSize = Mathf.Max(1f, (maxY - minY + 2f) / 2f,
            (maxX - minX + 2f) / (2f * camera.aspect));
    }

    public void UndoRoad()
    {
        route?.Undo();
        RefreshRoute();
    }

    public void ClearRoute()
    {
        route?.Clear();
        RefreshRoute();
    }

    public void ApplySuggestion()
    {
        if (route == null || suggestion == null) return;
        if (clock.Phase != DayPhase.Open) return;
        route.TryReplace(suggestion.EdgeIds);
        RefreshRoute();
    }

    private void RefreshRoute()
    {
        SelectedPlan = null;
        foreach (Button button in roadButtons)
        {
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
        roadButtons.Clear();
        roadContent.anchoredPosition = Vector2.zero;
        undoButton.interactable = clearButton.interactable = clock.Phase == DayPhase.Open && route != null && route.EdgeIds.Count > 0;
        suggestButton.interactable = clock.Phase == DayPhase.Open && suggestion != null;
        suggestButton.GetComponentInChildren<TextMeshProUGUI>().text = suggestion == null
            ? "Suggestion available after selecting orders / courier"
            : $"Optional suggestion: {clock.ToRealSeconds(suggestion.TotalTime):F1}s / Use this route";
        if (route == null) return;
        float skill = people.SelectedPerson?.SkillLevel ?? 1f;
        map.PreviewRoute(route.EdgeIds, route.CurrentNodeId, skill);
        int visited = orders.SelectedOrders.Count(o => route.HasVisited(o.DestinationNodeId));
        routeSummary.text = $"Current: {route.CurrentNodeId}    Time: {clock.ToRealSeconds(route.GetTime(skill)):F1}s\nDestinations visited: {visited} / {orders.SelectedOrders.Count}";
        nextRoadsTitle.text = "NEXT ROADS FROM " + route.CurrentNodeId;
        routePath.text = route.EdgeIds.Count == 0 ? "Start: " + map.StoreNodeId
            : string.Join("\n", route.EdgeIds.Select((id, i) =>
                $"{i + 1}. {route.NodeIds[i]} > {route.NodeIds[i + 1]} ({map.GetEdge(id).RoadType})"));
        routePath.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(92f,
            routePath.GetPreferredValues(routePath.text, routePath.rectTransform.rect.width, Mathf.Infinity).y);
        bool canDraw = clock.Phase == DayPhase.Open && orders.SelectedOrders.Count > 0 && people.SelectedPerson != null && people.SelectedPerson.IsAvailable;
        foreach (MapEdge edge in map.GetOutgoingEdges(route.CurrentNodeId))
        {
            edge.TryTraverse(route.CurrentNodeId, out string next);
            string roadName = edge.RoadType == RoadType.Backstreet ? "Backstreet" : "Road";
            Button button = Instantiate(roadOptionPrefab, roadContent);
            button.gameObject.name = "NextRoad_" + edge.Id;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"To {next} / {roadName} / {clock.ToRealSeconds(map.GetTravelTime(edge.Id, skill)):F1}s";
            label.fontSize = 19;
            label.color = new Color(0.1f, 0.14f, 0.2f);
            label.raycastTarget = false;
            button.onClick.AddListener(() => AppendRoad(edge.Id));
            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = 48;
            button.interactable = canDraw;
            roadButtons.Add(button);
        }
        SelectedPlan = route.BuildCompletion(orders.SelectedOrders, people.SelectedPerson);
        UpdateComparison(SelectedPlan, IndividualReferenceTime, orders.SelectedOrders.Count);
        if (clock.Phase == DayPhase.Open && orders.SelectedOrders.Count == 0 && dispatchedPlan != null)
        {
            UpdateComparison(dispatchedPlan, dispatchedReference, dispatchedPlan.Orders.Count);
            statusText.text = "Couriers are travelling. Select waiting orders and an idle courier.";
            return;
        }
        if (clock.Phase != DayPhase.Open) statusText.text = "Closed. Couriers will finish their trips before the result is shown.";
        else if (orders.OrderCount == 0) statusText.text = "Waiting for orders. New orders appear automatically during opening.";
        else if (!canDraw) statusText.text = "Select orders and an available courier, then click a road on the map.";
        else if (SelectedPlan != null) statusText.text = "Route ready! Dispatch, or keep editing. The clock is still running.";
        else if (visited < orders.SelectedOrders.Count)
        {
            string remaining = string.Join(", ", orders.SelectedOrders.Where(o => !route.HasVisited(o.DestinationNodeId)).Select(o => o.DestinationNodeId));
            statusText.text = "Visit: " + remaining + ". Click a connected road. You can return to Store along any connected route.";
        }
        else statusText.text = "All destinations visited. Draw the return route to " + map.StoreNodeId + ".";
    }

    private void UpdateComparison(DeliveryPlan plan, float? referenceTime, int orderCount)
    {
        comparisonText.color = new Color(0.87f, 0.91f, 0.97f);
        if (!referenceTime.HasValue)
        {
            comparisonText.text = "Individual vs your route: select orders and a courier.";
            return;
        }
        string baseline = $"Individual reference: {clock.ToRealSeconds(referenceTime.Value):F1}s ({orderCount} round trip(s))";
        if (plan == null)
        {
            comparisonText.text = baseline + "\nFinish your route and return to Store to compare.";
            return;
        }
        float saved = clock.ToRealSeconds(referenceTime.Value - plan.TotalTime);
        string difference = Mathf.Abs(saved) < 0.05f ? "Same time"
            : saved > 0 ? $"Saves {saved:F1}s" : $"Slower by {-saved:F1}s";
        comparisonText.text = baseline + $"\n{plan.Title}: {clock.ToRealSeconds(plan.TotalTime):F1}s / {difference}";
        if (saved > 0.05f) comparisonText.color = AccentColor;
        else if (saved < -0.05f) comparisonText.color = new Color(1f, 0.78f, 0.25f);
    }

}
