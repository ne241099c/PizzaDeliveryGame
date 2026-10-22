using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[Serializable] public sealed class CitySaveState {public bool generated;public int version=1,seed,blocks,day;}

public class MapManager : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField, Min(0.01f)] private float baseSpeed = 10f;
    [SerializeField, Min(0.01f), Tooltip("注文増加に合わせた全配達員共通の速度倍率")] private float deliverySpeedMultiplier=1.2f;
    public float DeliverySpeedMultiplier=>deliverySpeedMultiplier;
    [SerializeField, Min(0f)] private float backstreetPenalty = 1f;
    [SerializeField, Min(0.01f)] private float sampleBackstreetDistance = 11f;
    [SerializeField, Min(0.01f)] private float drivingSpeedBase = 0.7f;
    [SerializeField, Min(0f)] private float drivingSpeedScale = 0.6f;
    [SerializeField, Min(0.01f)] private float staminaSpeedBase = 0.7f;
    [SerializeField, Min(0f)] private float staminaSpeedScale = 0.3f;
    [SerializeField] private NodeView nodeViewPrefab;
    [SerializeField] private EdgeView edgeViewPrefab;
    [SerializeField] private Transform nodesParent;
    [SerializeField] private Transform edgesParent;
    [SerializeField] private MapDefinition mapDefinition;

    [SerializeField] private bool generateCampaignCity=true;
    [SerializeField] private CityGenerationSettings citySettings=new();
    public CityGenerationSettings CitySettings=>citySettings;
    public GeneratedCity City {get;private set;}
    public CitySaveState CityState {get;private set;}
    public bool UseSampleForValidation {get;set;}
    public int? SeedOverride {get;set;}
    public int? BlocksOverride {get;set;}
    public string TodayCityInfo=>City==null?"いつもの街で営業します。":$"街区{City.blocks.Count} / "+(City.construction.Count==0?"工事なし":$"{RoadCaption(City.construction[0])}は工事中・徐行（{citySettings.constructionDays}日ごとに更新）");
    public string RoadCaption(string edgeId)
    {
        if(City!=null){var block=City.blocks.FirstOrDefault(b=>edgeId.StartsWith($"B{b.index}_"));if(block!=null)return block.template.title+" "+(block.index+1)+"の通り";}return "駅前通り";
    }
    public string TomorrowCityInfo(int day)=>City!=null && citySettings.Blocks(day+1)>City.blocks.Count?"あした、新しい街区が開きます。":"あしたも同じ街で営業します。";
    public void NewCampaignCity()
    {
        CityState=generateCampaignCity && !UseSampleForValidation?new CitySaveState{generated=true,seed=SeedOverride??Guid.NewGuid().GetHashCode(),day=1}:null;
    }
    public CitySaveState CaptureCity()=>CityState==null?null:new CitySaveState{generated=true,seed=CityState.seed,blocks=CityState.blocks,day=CityState.day};
    public bool ValidCity(CitySaveState state,int day)=>state==null || !state.generated || state.version==1 && state.day==day && state.blocks==citySettings.Blocks(day);
    public void RestoreCity(CitySaveState state)
    {
        CityState=state==null || !state.generated?null:new CitySaveState{generated=true,seed=state.seed,blocks=state.blocks,day=state.day};
        PrepareCity(state?.day??1);
    }
    public void PrepareCity(int day)
    {
        if(!IsReady)return;
        if(UseSampleForValidation || CityState==null)
        {
            if(City!=null){var previousCity=City;City=null;LoadMap(null);Destroy(previousCity.map);}return;
        }
        int blocks=BlocksOverride??citySettings.Blocks(day);
        if(City!=null && CityState.blocks==blocks && City.seed==CityState.seed && City.day==day && City.blocks.Count==blocks)return;
        var old=City;var next=CityGenerator.Generate(CityState.seed,blocks,citySettings);
        if(day>=citySettings.constructionStartDay)
        {
            var candidates=next.map.Edges.Where(e=>e.Id.EndsWith("AB") && !next.bridges.Contains(e.Id)).ToArray();
            if(candidates.Length>0)
            {
                var e=candidates[(int)((uint)(CityState.seed^(day/citySettings.constructionDays))%(uint)candidates.Length)];float before=e.TrafficFactor;e.TrafficFactor=citySettings.constructionTraffic;
                var metrics=CityValidator.Measure(next,citySettings);if(metrics.failures.Count==0){next.construction.Add(e.Id);next.metrics=metrics;}else e.TrafficFactor=before;
            }
        }
        next.day=day;City=next;CityState.day=day;CityState.blocks=blocks;LoadMap(next.map);if(old!=null)Destroy(old.map);
    }
    public void LoadMap(MapDefinition definition)
    {
        IsReady=false;
        foreach(var view in nodeViews.Values){view.gameObject.SetActive(false);Destroy(view.gameObject);}foreach(var view in edgeViews.Values){view.gameObject.SetActive(false);Destroy(view.gameObject);}
        nodes.Clear();edges.Clear();nodeLookup.Clear();edgeLookup.Clear();outgoing.Clear();nodeViews.Clear();edgeViews.Clear();
        if(definition!=null){nodes.AddRange(definition.Nodes);edges.AddRange(definition.Edges);}else CreateMap();
        BuildViews();
    }
    private sealed class RenderChunk
    {
        public Bounds bounds;public readonly List<Renderer> renderers=new();
    }
    private readonly Dictionary<Vector2Int,RenderChunk> renderChunks=new();
    public int RenderChunkCount=>renderChunks.Count;
    public int MapRevision {get;private set;}
    private void IndexRenderChunks()
    {
        renderChunks.Clear();float size=citySettings.blockSize+citySettings.blockGap;
        foreach(var renderer in nodeViews.Values.SelectMany(v=>v.GetComponentsInChildren<Renderer>()).Concat(edgeViews.Values.SelectMany(v=>v.GetComponentsInChildren<Renderer>())).Where(r=>r.enabled))
        {
            var at=renderer.bounds.center;var key=new Vector2Int(Mathf.FloorToInt(at.x/size),Mathf.FloorToInt(at.y/size));
            if(!renderChunks.TryGetValue(key,out var chunk)){chunk=new RenderChunk{bounds=renderer.bounds};renderChunks.Add(key,chunk);}else chunk.bounds.Encapsulate(renderer.bounds);
            chunk.renderers.Add(renderer);
        }
    }
    private void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        foreach(var chunk in renderChunks.Values){bool visible=GeometryUtility.TestPlanesAABB(planes,chunk.bounds);foreach(var renderer in chunk.renderers)if(renderer!=null && renderer.enabled!=visible)renderer.enabled=visible;}
    }
    private readonly List<MapNode> nodes = new();
    private readonly List<MapEdge> edges = new();
    private readonly Dictionary<string, MapNode> nodeLookup = new();
    private readonly Dictionary<string, MapEdge> edgeLookup = new();
    private readonly Dictionary<string, List<MapEdge>> outgoing = new();
    private readonly Dictionary<string, EdgeView> edgeViews = new();
    private readonly Dictionary<string, NodeView> nodeViews = new();
    private readonly RouteState routeState = new();

    public event Action<RouteState> OnRouteChanged;
    public event Action MapReady;
    public event Action<string> EdgeClicked;
    public RouteState CurrentRouteState => routeState;
    public bool IsReady { get; private set; }
    /// <summary>The shop's dispatch board handles pointer input itself (drag, zoom, pin taps).</summary>
    public bool PointerInputEnabled { get; set; } = true;
    public string StoreNodeId { get; private set; }
    public IReadOnlyList<MapNode> Nodes => nodes;
    public IReadOnlyList<MapEdge> Edges => edges;

    private void Start()
    {
        CreateMap();
        BuildViews();
    }
    private void BuildViews()
    {
        ValidateAndIndex();
        foreach(var node in nodes){var view=Instantiate(nodeViewPrefab,nodesParent);view.Initialize(node);nodeViews.Add(node.Id,view);}
        foreach(var edge in edges){var view=Instantiate(edgeViewPrefab,edgesParent);view.Initialize(edge,nodeLookup[edge.NodeAId],nodeLookup[edge.NodeBId],this,false);edgeViews.Add(edge.Id,view);}
        IndexRenderChunks();MapRevision++;IsReady=true;PreviewRoute(Array.Empty<string>(),StoreNodeId,1);MapReady?.Invoke();
    }

    private void Update()
    {
        if (!IsReady || Mouse.current == null || !PointerInputEnabled) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.middleButton.isPressed
            && Mouse.current.scroll.ReadValue().y == 0) return;
        Vector2 position = Mouse.current.position.ReadValue();
        Camera camera = Camera.main;
        if (camera != null && camera.pixelRect.Contains(position) && !IsOverUI(position))
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
                camera.orthographicSize = Mathf.Clamp(camera.orthographicSize * Mathf.Pow(0.85f, Mathf.Clamp(scroll / 120f, -1f, 1f)), 0.5f, 10000f);
            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                float scale = camera.orthographicSize * 2f / camera.pixelHeight;
                camera.transform.position -= new Vector3(delta.x, delta.y, 0) * scale;
            }
        }
        if (Mouse.current.leftButton.wasPressedThisFrame) HandleMapClick(position);
    }

    public bool HandleMapClick(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (!IsReady || camera == null || !camera.pixelRect.Contains(screenPosition)) return false;
        if (IsOverUI(screenPosition)) return false;
        // Choose one nearest road per click. Parallel roads cannot both be appended by one input.
        EdgeView nearest = null;
        float bestDistance = 10f;
        foreach (EdgeView view in edgeViews.Values)
        {
            float distance = view.DistanceToScreenPoint(camera, screenPosition);
            if (distance < bestDistance) { bestDistance = distance; nearest = view; }
        }
        if (nearest == null) return false;
        EdgeClicked?.Invoke(nearest.EdgeId);
        return true;
    }

    private static bool IsOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Any(h => h.gameObject.GetComponentInParent<Canvas>() != null);
    }

    public MapEdge GetEdge(string edgeId)
    {
        if (!edgeLookup.TryGetValue(edgeId, out MapEdge edge)) throw new ArgumentException("Unknown road: " + edgeId);
        return edge;
    }

    public bool HasNode(string nodeId) => nodeId != null && nodeLookup.ContainsKey(nodeId);
    public IReadOnlyList<MapEdge> GetOutgoingEdges(string nodeId) => outgoing[nodeId];

    public float GetDrivingSpeedFactor(DeliveryPerson person)
        => drivingSpeedBase + drivingSpeedScale * Mathf.Clamp01(person.Driving);

    public float GetStaminaSpeedFactor(DeliveryPerson person)
        => staminaSpeedBase + staminaSpeedScale * Mathf.Clamp01(person.CurrentStamina);

    public float GetTravelTime(string edgeId, DeliveryPerson person)
        => GetTravelTime(edgeId, person.LocalKnowledge) / (GetDrivingSpeedFactor(person) * GetStaminaSpeedFactor(person));

    public float GetTravelTime(string edgeId, float skillLevel = 1f)
    {
        MapEdge edge = GetEdge(edgeId);
        float time = edge.Distance / (baseSpeed * deliverySpeedMultiplier * edge.TrafficFactor * (facilities!=null?facilities.SpeedMultiplier:1));
        if (edge.RoadType == RoadType.Backstreet)
            time *= 1f + (1f - Mathf.Clamp01(skillLevel)) * backstreetPenalty;
        return time;
    }

    public void PreviewRoute(IReadOnlyList<string> edgeIds, string currentNodeId, float skillLevel)
    {
        routeState.ActiveEdgeIds.Clear();
        routeState.ActiveEdgeIds.AddRange(edgeIds);
        routeState.TotalDistance = edgeIds.Sum(id => GetEdge(id).Distance);
        routeState.EstimatedTime = edgeIds.Sum(id => GetTravelTime(id, skillLevel));
        string lastEdgeId = edgeIds.Count == 0 ? null : edgeIds[edgeIds.Count - 1];
        foreach (MapEdge edge in edges)
            edgeViews[edge.Id].SetRouteStyle(edgeIds.Contains(edge.Id),
                edge.TryTraverse(currentNodeId, out _), edge.Id == lastEdgeId);
        foreach (var pair in nodeViews) pair.Value.SetCurrent(pair.Key == currentNodeId);
        OnRouteChanged?.Invoke(routeState);
    }

    private void ValidateAndIndex()
    {
        if (baseSpeed <= 0 || backstreetPenalty < 0) throw new InvalidOperationException("Invalid speed or penalty.");
        if (nodes.Count(n => n.IsStore) != 1) throw new InvalidOperationException("A map needs exactly one store.");
        foreach (MapNode node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || nodeLookup.ContainsKey(node.Id))
                throw new InvalidOperationException("Node IDs must be non-empty and unique.");
            nodeLookup.Add(node.Id, node);
            outgoing.Add(node.Id, new List<MapEdge>());
        }
        StoreNodeId = nodes.Single(n => n.IsStore).Id;
        foreach (MapEdge edge in edges)
        {
            if (string.IsNullOrWhiteSpace(edge.Id) || edgeLookup.ContainsKey(edge.Id)
                || !HasNode(edge.NodeAId) || !HasNode(edge.NodeBId)
                || edge.Distance <= 0 || edge.TrafficFactor <= 0
                || float.IsNaN(edge.Distance) || float.IsInfinity(edge.Distance)
                || float.IsNaN(edge.TrafficFactor) || float.IsInfinity(edge.TrafficFactor))
                throw new InvalidOperationException("Invalid road: " + edge.Id);
            edgeLookup.Add(edge.Id, edge);
            outgoing[edge.NodeAId].Add(edge);
            if (edge.Bidirectional && edge.NodeBId != edge.NodeAId) outgoing[edge.NodeBId].Add(edge);
        }
    }

    private void CreateMap()
    {
        if (mapDefinition != null)
        {
            nodes.AddRange(mapDefinition.Nodes);
            edges.AddRange(mapDefinition.Edges);
            return;
        }
        nodes.Add(new MapNode("Store", new Vector2(-4f, 0f), NodeType.Intersection, true));
        nodes.Add(new MapNode("N1", new Vector2(-1.5f, 0f), NodeType.Intersection, false));
        nodes.Add(new MapNode("N2", new Vector2(1.5f, 0f), NodeType.Intersection, false));
        nodes.Add(new MapNode("Goal1", new Vector2(4f, -1f), NodeType.DeliveryAddress, false));
        nodes.Add(new MapNode("Goal2", new Vector2(4f, -3f), NodeType.DeliveryAddress, false));
        edges.Add(new MapEdge("Store-N1", "Store", "N1", 10f, RoadType.Main, 0.8f));
        edges.Add(new MapEdge("N1-N2-Main", "N1", "N2", 10f, RoadType.Main, 0.6f));
        edges.Add(new MapEdge("N1-N2-Backstreet", "N1", "N2", sampleBackstreetDistance, RoadType.Backstreet, 1f));
        edges.Add(new MapEdge("N2-Goal1", "N2", "Goal1", 5f, RoadType.Main, 1f));
        edges.Add(new MapEdge("N2-Goal2", "N2", "Goal2", 7f, RoadType.Main, 1f));
        edges.Add(new MapEdge("Goal1-Goal2", "Goal1", "Goal2", 3f, RoadType.Main, 1f));
    }
}
