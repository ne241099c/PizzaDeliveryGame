using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Authoring tool: builds editable Scene objects once, never from runtime Awake/Start.</summary>
public static class M4SceneSetup
{
    private static readonly Color PanelColor = new(0.09f, 0.13f, 0.20f, 1f);
    private static readonly Color CardColor = new(0.16f, 0.21f, 0.30f, 1f);
    private static readonly Color Accent = new(0.32f, 0.88f, 0.69f, 1f);
    private static readonly Dictionary<string, Object> refs = new();

    [MenuItem("Tools/Pizza Delivery/Build M4 Scene UI")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new System.InvalidOperationException("Open SampleScene first.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build M4 scene UI");
        refs.Clear();
        var manager = GameObject.Find("GameManager");
        var map = Object.FindFirstObjectByType<MapManager>();
        var clock = Ensure<GameClock>(manager);
        var stats = Ensure<DayStats>(manager);
        var spawner = Ensure<OrderSpawner>(manager);
        var markers = Ensure<DeliveryMarkerView>(manager);
        var ui = manager.GetComponent<DeliveryPlannerUI>();
        var orders = manager.GetComponent<OrderListUI>();
        var people = manager.GetComponent<DeliveryPersonSelector>();
        var flow = manager.GetComponent<DeliveryFlowController>();
        var score = manager.GetComponent<ScoreManager>();
        var oldOrder = new SerializedObject(orders).FindProperty("orderItemPrefab").objectReferenceValue as Button;
        var oldPerson = new SerializedObject(people).FindProperty("deliveryPersonItemPrefab").objectReferenceValue as Button;
        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        foreach (Transform child in canvas.transform.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
        Undo.RecordObject(canvas, "Configure Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvas.GetComponent<CanvasScaler>();
        Undo.RecordObject(scaler, "Configure scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvas.transform;
        Panel(root, "TopBackdrop", 0, 0, 1280, 209);
        Panel(root, "BottomBackdrop", 0, 540, 1280, 180);
        Panel(root, "LeftBackdrop", 0, 209, 320, 331);
        Panel(root, "RightBackdrop", 717, 209, 563, 331);
        var header = Panel(root, "Header", 0, 0, 1280, 88);
        Text(header, "Title", "PIZZA DELIVERY", 24, 12, 600, 34, 30, Accent);
        Text(header, "Subtitle", "Orders arrive automatically. Choose orders, an idle courier, then dispatch.", 24, 49, 1100, 25, 18);
        Text(header, "BusinessClock", "OPEN 05:00", 640, 12, 380, 34, 25, Accent);
        var scoreLabel = Text(header, "PlannerScore", "Score: 0", 1030, 14, 225, 34, 27);
        scoreLabel.alignment = TextAlignmentOptions.Right;

        var left = Panel(root, "Selections", 20, 104, 282, 506);
        Text(left, "OrdersTitle", "1 / ORDERS (select up to 2)", 16, 14, 254, 28, 19, Accent);
        RectTransform orderContent = ScrollContent(left, "Orders", 16, 53, 250, 160, 8);
        Text(left, "OrdersEmpty", "Waiting for new orders...", 20, 62, 242, 60, 18).gameObject.SetActive(false);
        PreviewButton(oldOrder, orderContent, "OrderPreview1", "Order row / deadline", 62);
        PreviewButton(oldOrder, orderContent, "OrderPreview2", "Order row / deadline", 62);
        Text(left, "CouriersTitle", "2 / COURIER", 16, 225, 250, 28, 19, Accent);
        var peopleContent = Rect(left, "PlannerPersonContent", 16, 264, 250, 234);
        Vertical(peopleContent, 7);
        foreach (var person in people.People)
            PreviewButton(oldPerson, peopleContent, "CourierPreview_" + person.Name,
                person.Name + $" / Idle\n<size=14>Drive {person.Driving:F1} Know {person.LocalKnowledge:F1} Load {person.Capacity}"
                + $"\nService {person.Service:F1} Endurance {person.Endurance:F1} Safety {person.Safety:F1}</size>", 52);

        Text(root, "MapTitle", "DRAW YOUR ROUTE", 324, 108, 310, 28, 21, Accent);
        Button(root, "FitMap", "Fit", 646, 106, 68, 32);
        Text(root, "MapLegend", "Click a road connected to the current node (*).\nBlue: next / Green: used / Yellow: last\nScroll: zoom / Middle-drag: pan", 324, 145, 390, 59, 16);
        Button(root, "UndoRoad", "Undo last road", 324, 550, 190, 50);
        Button(root, "ClearRoute", "Clear route", 524, 550, 190, 50);
        var right = Panel(root, "RouteEditor", 736, 104, 524, 506);
        Text(right, "RouteTitle", "3 / YOUR ROUTE", 16, 14, 492, 28, 19, Accent);
        Text(right, "RouteSummary", "Current: Store / Time: 0.0s\nDestinations visited: 0 / 0", 16, 50, 492, 55, 20);
        Text(right, "IndividualComparison", "Select orders and a courier to compare times.", 16, 110, 492, 58, 18);
        Text(right, "NextRoadsTitle", "NEXT ROADS FROM STORE", 16, 176, 492, 26, 18, Accent);
        RectTransform roadContent = ScrollContent(right, "Road", 12, 210, 500, 112, 7);
        PreviewButton(oldOrder, roadContent, "RoadPreview", "To N1 / Road", 48);
        RectTransform history = ScrollContent(right, "History", 16, 330, 492, 93, 0);
        var historyText = Text(history, "RoutePath", "Start: Store", 0, 0, 492, 20, 18);
        historyText.alignment = TextAlignmentOptions.TopLeft;
        historyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 92;
        Button(right, "UseSuggestedRoute", "Optional suggestion / select orders and courier", 16, 438, 492, 52);
        Text(root, "Activity", "Arrivals and late delivery results appear here.", 324, 605, 936, 23, 17, Accent);
        var footer = Panel(root, "Footer", 0, 628, 1280, 92);
        Text(footer, "Status", "Wait for orders, then choose an idle courier and draw a route.", 24, 12, 650, 67, 18);
        Button(footer, "DispatchRoute", "Dispatch", 700, 18, 160, 56);
        Button(footer, "DispatchSuggestion", "Dispatch with suggestion", 875, 18, 275, 56);
        Button(footer, "RestartDemo", "Reset", 1160, 18, 96, 56);

        var results = Panel(root, "DayResults", 0, 0, 1280, 720);
        results.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.09f, 0.97f);
        results.GetComponent<Image>().raycastTarget = true;
        var card = Panel(results, "ResultCard", 340, 120, 600, 480);
        var result = Text(card, "ResultText", "DAY COMPLETE\n\nScore: 0\nDelivered: 0\nLate: 0\nCancelled: 0", 40, 28, 520, 320, 30);
        result.alignment = TextAlignmentOptions.Center;
        Button(card, "ReplayDay", "Play again", 140, 375, 320, 64);
        results.gameObject.SetActive(false);

        Bind(ui, "orders", orders, "people", people, "map", map, "clock", clock, "flow", flow, "stats", stats,
            "statusText", refs["Status"], "routeSummary", refs["RouteSummary"], "comparisonText", refs["IndividualComparison"],
            "routePath", refs["RoutePath"], "nextRoadsTitle", refs["NextRoadsTitle"], "clockText", refs["BusinessClock"],
            "activityText", refs["Activity"], "resultText", refs["ResultText"], "roadContent", roadContent, "roadOptionPrefab", oldOrder,
            "undoButton", refs["UndoRoad"], "clearButton", refs["ClearRoute"], "suggestButton", refs["UseSuggestedRoute"],
            "dispatchButton", refs["DispatchRoute"], "dispatchSuggestionButton", refs["DispatchSuggestion"],
            "resetButton", refs["RestartDemo"], "fitButton", refs["FitMap"], "replayButton", refs["ReplayDay"], "resultsPanel", results.gameObject);
        Bind(orders, "orderListContent", orderContent, "clock", clock, "emptyMessage", refs["OrdersEmpty"]);
        Bind(people, "deliveryPersonListContent", peopleContent, "clock", clock);
        Bind(score, "scoreText", scoreLabel);
        Bind(flow, "scoreManager", score, "orderListUI", orders, "deliveryPersonSelector", people, "clock", clock, "stats", stats);
        Bind(spawner, "clock", clock, "map", map, "orders", orders);
        Transform markersRoot = map.transform.Find("CourierMarkers");
        if (markersRoot == null) markersRoot = New("CourierMarkers", map.transform).transform;
        Bind(markers, "flow", flow, "clock", clock, "markersParent", markersRoot);
        CreateMapPreview(map);
        Camera camera = Camera.main;
        Undo.RecordObject(camera, "Map viewport");
        Undo.RecordObject(camera.transform, "Map position");
        camera.rect = new Rect(0.25f, 0.25f, 0.31f, 0.46f);
        camera.orthographicSize = 4.5f;
        camera.transform.position = new Vector3(0, -1.8f, -10);
        camera.backgroundColor = new Color(0.045f, 0.07f, 0.12f);
        M4StageSceneSetup.AddPromiseUI();
        M4StageSceneSetup.AddReputationUI();
        M4StageSceneSetup.AddDynamics();
        M4StageSceneSetup.AddRosterScroll();
        Canvas.ForceUpdateCanvases();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("M4 scene authored. All controls are now editable under Canvas. Save the scene after inspection.");
    }

    private static void CreateMapPreview(MapManager map)
    {
        var old = map.transform.Find("EditModePreview");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = New("EditModePreview", map.transform);
        root.AddComponent<EditorPreviewOnly>();
        var so = new SerializedObject(map);
        var definition = so.FindProperty("mapDefinition").objectReferenceValue as MapDefinition;
        var nodes = definition != null ? definition.Nodes : new List<MapNode> {
            new("Store",new Vector2(-4,0),NodeType.Intersection,true), new("N1",new Vector2(-1.5f,0),NodeType.Intersection,false),
            new("N2",new Vector2(1.5f,0),NodeType.Intersection,false), new("Goal1",new Vector2(4,-1),NodeType.DeliveryAddress,false),
            new("Goal2",new Vector2(4,-3),NodeType.DeliveryAddress,false) };
        var edges = definition != null ? definition.Edges : new List<MapEdge> {
            new("Store-N1","Store","N1",10,RoadType.Main,0.8f), new("N1-N2-Main","N1","N2",10,RoadType.Main,0.6f),
            new("N1-N2-Backstreet","N1","N2",13,RoadType.Backstreet,1), new("N2-Goal1","N2","Goal1",5,RoadType.Main,1),
            new("N2-Goal2","N2","Goal2",7,RoadType.Main,1), new("Goal1-Goal2","Goal1","Goal2",3,RoadType.Main,1) };
        var nodePrefab = so.FindProperty("nodeViewPrefab").objectReferenceValue as NodeView;
        var edgePrefab = so.FindProperty("edgeViewPrefab").objectReferenceValue as EdgeView;
        foreach (var node in nodes)
        {
            var go = PrefabUtility.InstantiatePrefab(nodePrefab.gameObject, root.transform) as GameObject;
            go.GetComponent<NodeView>().Initialize(node);
        }
        foreach (var edge in edges)
        {
            var go = PrefabUtility.InstantiatePrefab(edgePrefab.gameObject, root.transform) as GameObject;
            go.GetComponent<EdgeView>().Initialize(edge, nodes.First(n=>n.Id==edge.NodeAId), nodes.First(n=>n.Id==edge.NodeBId), map, false);
        }
    }

    private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static void Bind(Object target, params object[] pairs)
    {
        Undo.RecordObject(target, "Assign scene references");
        var so = new SerializedObject(target);
        for (int i = 0; i < pairs.Length; i += 2)
        {
            var property = so.FindProperty((string)pairs[i]);
            if (property == null) throw new System.Exception("Missing serialized field: " + pairs[i]);
            property.objectReferenceValue = pairs[i + 1] as Object;
        }
        so.ApplyModifiedProperties();
    }
    private static GameObject New(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, "Create M4 UI");
        go.transform.SetParent(parent, false);
        return go;
    }
    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = New(name, parent, typeof(RectTransform)); go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h);
        refs[name] = rect;
        return rect;
    }
    private static RectTransform Panel(Transform parent, string name, float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,x,y,w,h); var image=rect.gameObject.AddComponent<Image>();
        image.color=PanelColor; image.raycastTarget=false; return rect;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,Color? color=null)
    {
        var rect=Rect(parent,name,x,y,w,h);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text=value;label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.color=color??new Color(0.87f,0.91f,0.97f);
        label.alignment=TextAlignmentOptions.MidlineLeft;label.textWrappingMode=TextWrappingModes.Normal;label.raycastTarget=false;
        refs[name]=label; return label;
    }
    private static Button Button(Transform parent,string name,string value,float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=CardColor;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
        Text(rect,name+"Label",value,10,4,w-20,h-8,19);refs[name]=button;return button;
    }
    private static void Vertical(RectTransform content,float spacing)
    {
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=spacing;
        layout.childControlWidth=layout.childControlHeight=layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        layout.childAlignment=TextAnchor.UpperLeft;
    }
    private static RectTransform ScrollContent(Transform parent,string name,float x,float y,float w,float h,float spacing)
    {
        var viewport=Rect(parent,name+"Viewport",x,y,w,h);viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
        var content=Rect(viewport,name+"Content",0,0,w,0);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.sizeDelta=Vector2.zero;
        Vertical(content,spacing);content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content;scroll.viewport=viewport;return content;
    }
    private static void PreviewButton(Button prefab,Transform parent,string name,string text,float height)
    {
        var go=PrefabUtility.InstantiatePrefab(prefab.gameObject,parent) as GameObject;go.name=name;
        var layout=go.GetComponent<LayoutElement>()??go.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=height;
        var label=go.GetComponentInChildren<TextMeshProUGUI>();label.text=text;label.fontSize=19;label.raycastTarget=false;
    }
}
