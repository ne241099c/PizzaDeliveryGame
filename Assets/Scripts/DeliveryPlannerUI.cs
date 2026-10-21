using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the M3 demo UI at Play start; existing order/person prefabs remain in use.</summary>
[DefaultExecutionOrder(-200)]
public class DeliveryPlannerUI : MonoBehaviour
{
    private OrderListUI orders;
    private DeliveryPersonSelector people;
    private MapManager map;
    private ManualDeliveryRoute route;
    private RouteSuggestion suggestion;
    private DeliveryPlan completedPlan;
    private float? completedReferenceTime;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI routeSummary;
    private TextMeshProUGUI comparisonText;
    private TextMeshProUGUI routePath;
    private TextMeshProUGUI nextRoadsTitle;
    private RectTransform roadContent;
    private readonly List<Button> roadButtons = new();
    private Button undoButton;
    private Button clearButton;
    private Button suggestButton;
    private bool subscribed;
    public DeliveryPlan SelectedPlan { get; private set; }
    public ManualDeliveryRoute Route => route;
    public float? IndividualReferenceTime { get; private set; }

    private static readonly Color PanelColor = new(0.09f, 0.13f, 0.20f, 1f);
    private static readonly Color CardColor = new(0.16f, 0.21f, 0.30f, 1f);
    private static readonly Color AccentColor = new(0.32f, 0.88f, 0.69f, 1f);

    private void Awake()
    {
        orders = GetComponent<OrderListUI>();
        people = GetComponent<DeliveryPersonSelector>();
        map = FindFirstObjectByType<MapManager>();
        // Replace the old overlapping presentation for this scene at runtime.
        Canvas oldCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (oldCanvas != null) oldCanvas.gameObject.SetActive(false);
        BuildUI();
    }

    private void Start()
    {
        orders.SelectionChanged += OnOrdersChanged;
        people.SelectionChanged += OnPersonChanged;
        map.MapReady += OnMapReady;
        map.EdgeClicked += AppendRoad;
        subscribed = true;
        if (map.IsReady) OnMapReady();
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        orders.SelectionChanged -= OnOrdersChanged;
        people.SelectionChanged -= OnPersonChanged;
        map.MapReady -= OnMapReady;
        map.EdgeClicked -= AppendRoad;
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
            completedPlan = null;
            completedReferenceTime = null;
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

    private void BuildUI()
    {
        var root = new GameObject("M3 Planner Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas plannerCanvas = root.GetComponent<Canvas>();
        plannerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        plannerCanvas.sortingOrder = 10;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform canvas = root.GetComponent<RectTransform>();

        // The map camera renders only its viewport; cover the surrounding screen each frame.
        Panel(canvas, "TopBackdrop", 0, 0, 1280, 209);
        Panel(canvas, "BottomBackdrop", 0, 540, 1280, 180);
        Panel(canvas, "LeftBackdrop", 0, 209, 320, 331);
        Panel(canvas, "RightBackdrop", 717, 209, 563, 331);

        RectTransform header = Panel(canvas, "Header", 0, 0, 1280, 88);
        Text(header, "Title", "PIZZA DELIVERY", 24, 12, 600, 34, 30, AccentColor);
        Text(header, "Subtitle", "Choose orders and a courier. Click roads to draw your own route.", 24, 49, 850, 25, 19);
        var score = Text(header, "PlannerScore", "Score: 0", 1020, 21, 235, 42, 30);
        score.alignment = TextAlignmentOptions.Right;
        GetComponent<ScoreManager>().BindText(score);

        RectTransform left = Panel(canvas, "Selections", 20, 104, 282, 506);
        Text(left, "OrdersTitle", "1 / ORDERS (select up to 2)", 16, 14, 254, 28, 19, AccentColor);
        RectTransform orderContent = Rect(left, "PlannerOrderContent", 16, 54, 250, 144);
        AddVerticalLayout(orderContent, 10);
        orders.ConfigureContent(orderContent);
        Text(left, "CouriersTitle", "2 / COURIER", 16, 220, 250, 28, 19, AccentColor);
        RectTransform peopleContent = Rect(left, "PlannerPersonContent", 16, 260, 250, 166);
        AddVerticalLayout(peopleContent, 10);
        people.ConfigureContent(peopleContent);
        Text(left, "SkillHint", "Higher skill reduces backstreet travel time.", 16, 443, 250, 48, 18);

        Text(canvas, "MapTitle", "DRAW YOUR ROUTE", 324, 108, 310, 28, 21, AccentColor);
        Button(canvas, "FitMap", "Fit", 646, 106, 68, 32, FitMapCamera);
        Text(canvas, "MapLegend", "Click a road connected to the current node (*).\nBlue: next / Green: used / Yellow: last\nScroll: zoom / Middle-drag: pan", 324, 145, 390, 59, 16);
        undoButton = Button(canvas, "UndoRoad", "Undo last road", 324, 550, 190, 50, UndoRoad);
        clearButton = Button(canvas, "ClearRoute", "Clear route", 524, 550, 190, 50, ClearRoute);

        RectTransform right = Panel(canvas, "RouteEditor", 736, 104, 524, 506);
        Text(right, "RouteTitle", "3 / YOUR ROUTE", 16, 14, 492, 28, 19, AccentColor);
        routeSummary = Text(right, "RouteSummary", "", 16, 50, 492, 55, 20);
        comparisonText = Text(right, "IndividualComparison", "", 16, 110, 492, 58, 18);
        nextRoadsTitle = Text(right, "NextRoadsTitle", "Next roads", 16, 176, 492, 26, 18, AccentColor);
        RectTransform viewport = Rect(right, "RoadViewport", 12, 210, 500, 112);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28;
        roadContent = Rect(viewport, "NextRoadContent", 0, 0, 500, 0);
        roadContent.anchorMin = new Vector2(0, 1);
        roadContent.anchorMax = new Vector2(1, 1);
        roadContent.sizeDelta = Vector2.zero;
        AddVerticalLayout(roadContent, 7);
        roadContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = roadContent;
        scroll.viewport = viewport;
        RectTransform historyViewport = Rect(right, "RouteHistory", 16, 330, 492, 93);
        historyViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        historyViewport.gameObject.AddComponent<RectMask2D>();
        var historyScroll = historyViewport.gameObject.AddComponent<ScrollRect>();
        historyScroll.horizontal = false;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.scrollSensitivity = 25;
        routePath = Text(historyViewport, "RoutePath", "", 0, 0, 492, 0, 18);
        routePath.alignment = TextAlignmentOptions.TopLeft;
        routePath.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        historyScroll.viewport = historyViewport;
        historyScroll.content = routePath.rectTransform;
        suggestButton = Button(right, "UseSuggestedRoute", "", 16, 438, 492, 52, ApplySuggestion);
        suggestButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;

        RectTransform footer = Panel(canvas, "Footer", 0, 628, 1280, 92);
        statusText = Text(footer, "Status", "Select orders and a courier, then click roads on the map.", 24, 12, 810, 67, 19);
        Button complete = Button(footer, "CompleteSelectedPlan", "Complete route", 856, 18, 290, 56, null);
        GetComponent<DeliveryFlowController>().ConfigureUI(complete, this);
        Button restart = Button(footer, "RestartDemo", "Reset", 1160, 18, 96, 56, null);
        restart.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.rect = new Rect(0.25f, 0.25f, 0.31f, 0.46f);
            camera.orthographicSize = 4.5f;
            camera.transform.position = new Vector3(0, -1.3f, -10);
            camera.backgroundColor = new Color(0.045f, 0.07f, 0.12f);
        }
    }

    public void AppendRoad(string edgeId)
    {
        if (route == null || orders.SelectedOrders.Count == 0 || people.SelectedPerson == null)
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
        undoButton.interactable = clearButton.interactable = route != null && route.EdgeIds.Count > 0;
        suggestButton.interactable = suggestion != null;
        suggestButton.GetComponentInChildren<TextMeshProUGUI>().text = suggestion == null
            ? "Suggestion available after selecting orders / courier"
            : $"Optional suggestion: {suggestion.TotalTime:F2}s / Use this route";
        if (route == null) return;
        float skill = people.SelectedPerson?.SkillLevel ?? 1f;
        map.PreviewRoute(route.EdgeIds, route.CurrentNodeId, skill);
        int visited = orders.SelectedOrders.Count(o => route.HasVisited(o.DestinationNodeId));
        routeSummary.text = $"Current: {route.CurrentNodeId}    Time: {route.GetTime(skill):F2}s\nDestinations visited: {visited} / {orders.SelectedOrders.Count}";
        nextRoadsTitle.text = "NEXT ROADS FROM " + route.CurrentNodeId;
        routePath.text = route.EdgeIds.Count == 0 ? "Start: " + map.StoreNodeId
            : string.Join("\n", route.EdgeIds.Select((id, i) =>
                $"{i + 1}. {route.NodeIds[i]} > {route.NodeIds[i + 1]} ({map.GetEdge(id).RoadType})"));
        bool canDraw = orders.SelectedOrders.Count > 0 && people.SelectedPerson != null && people.SelectedPerson.IsAvailable;
        foreach (MapEdge edge in map.GetOutgoingEdges(route.CurrentNodeId))
        {
            edge.TryTraverse(route.CurrentNodeId, out string next);
            string roadName = edge.RoadType == RoadType.Backstreet ? "Backstreet" : "Road";
            Button button = Button(roadContent, "NextRoad_" + edge.Id,
                $"To {next} / {roadName} / {map.GetTravelTime(edge.Id, skill):F2}s", 0, 0, 500, 48,
                () => AppendRoad(edge.Id));
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = 48;
            button.interactable = canDraw;
            roadButtons.Add(button);
        }
        SelectedPlan = route.BuildCompletion(orders.SelectedOrders, people.SelectedPerson);
        UpdateComparison(SelectedPlan, IndividualReferenceTime, orders.SelectedOrders.Count);
        if (orders.SelectedOrders.Count == 0 && completedPlan != null)
        {
            ShowCompletion(completedPlan, completedReferenceTime);
            return;
        }
        if (orders.OrderCount == 0) statusText.text = "All deliveries completed! Reset to play again.";
        else if (!canDraw) statusText.text = "Select orders and an available courier, then click a road on the map.";
        else if (SelectedPlan != null) statusText.text = "Route ready! Complete route, or keep editing the roads.";
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
        string baseline = $"Individual reference: {referenceTime.Value:F2}s ({orderCount} round trip(s))";
        if (plan == null)
        {
            comparisonText.text = baseline + "\nFinish your route and return to Store to compare.";
            return;
        }
        float saved = referenceTime.Value - plan.TotalTime;
        string difference = Mathf.Abs(saved) < 0.005f ? "Same time"
            : saved > 0 ? $"Saves {saved:F2}s" : $"Slower by {-saved:F2}s";
        comparisonText.text = baseline + $"\n{plan.Title}: {plan.TotalTime:F2}s / {difference}";
        if (saved > 0.005f) comparisonText.color = AccentColor;
        else if (saved < -0.005f) comparisonText.color = new Color(1f, 0.78f, 0.25f);
    }

    public void ShowCompletion(DeliveryPlan plan, float? individualReferenceTime)
    {
        completedPlan = plan;
        completedReferenceTime = individualReferenceTime;
        UpdateComparison(plan, individualReferenceTime, plan.Orders.Count);
        int points = plan.Orders.Count * GetComponent<ScoreManager>().ScorePerDelivery;
        statusText.text = $"Completed {plan.Orders.Count} order(s)! +{points} points. Route time: {plan.TotalTime:F2}s.\n"
            + (orders.OrderCount == 0 ? "All orders delivered. Reset to play again." : "Select the next order and courier.");
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height)
    {
        RectTransform rect = Rect(parent, name, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y,
        float width, float height, float size, Color? color = null)
    {
        RectTransform rect = Rect(parent, name, x, y, width, height);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.color = color ?? new Color(0.87f, 0.91f, 0.97f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(Transform parent, string name, string value, float x, float y,
        float width, float height, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Rect(parent, name, x, y, width, height);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = CardColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (action != null) button.onClick.AddListener(action);
        Text(rect, "Label", value, 10, 4, width - 20, height - 8, 20);
        return button;
    }

    private static void AddVerticalLayout(RectTransform rect, float spacing)
    {
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
    }
}
