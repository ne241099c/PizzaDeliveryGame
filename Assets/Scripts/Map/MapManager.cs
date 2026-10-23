using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[Serializable] public sealed class CitySaveState {public bool generated;public int version=CityGenerator.CurrentVersion,seed,blocks,day,housesPerBlock;}

public class MapManager : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField, Min(0.01f)] private float baseSpeed = 10f;
    [SerializeField, Min(0.01f), Tooltip("注文増加に合わせた全配達員共通の速度倍率")] private float deliverySpeedMultiplier=1.6f;
    public float DeliverySpeedMultiplier=>deliverySpeedMultiplier;
    [SerializeField, Min(0f), Tooltip("裏道の描画上のふくらみ。距離・配達時間は変えません。")] private float backstreetVisualOffset=.85f;
    public float BackstreetVisualOffset=>backstreetVisualOffset;
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
    [SerializeField, Tooltip("旧バージョン3の街は、既存の家と道を保ったまま家を追加します。 ")] private bool expandExistingCityHomes=true;
    public bool ExpandExistingCityHomes=>expandExistingCityHomes;
    [SerializeField, Tooltip("続きからの開店準備で、古い街を店舗周囲の新しい道路網に更新します。資金・従業員・営業日を維持します。 ")] private bool upgradeRestoredCityLayout=true;
    public bool UpgradeRestoredCityLayout {get=>upgradeRestoredCityLayout;set=>upgradeRestoredCityLayout=value;}
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
        if(City!=null)
        {
            var block=City.blocks.FirstOrDefault(b=>edgeId.StartsWith($"B{b.index}_"));
            if(block!=null)return block.template.title+" "+(block.index+1)+"の通り";
        }
        if(City!=null && City.version>=7)
        {
            var road=GetEdge(edgeId);int index=edges.FindIndex(e=>e.Id==edgeId)+1;
            return (road.RoadType==RoadType.Backstreet?"裏道":edgeId.Contains("_Local")?"生活道路":"通り")+" "+index;
        }
        return "駅前通り";
    }
    public string TomorrowCityInfo(int day)=>City!=null && citySettings.BlocksForVersion(day+1,CityState.version)>City.blocks.Count?"あした、新しい街区が開きます。":"あしたも同じ街で営業します。";
    public void NewCampaignCity()
    {
        CityState=generateCampaignCity && !UseSampleForValidation?new CitySaveState{generated=true,housesPerBlock=citySettings.HousesFor(CityGenerator.CurrentVersion,0),seed=SeedOverride??Guid.NewGuid().GetHashCode(),day=1}:null;
    }
    public CitySaveState CaptureCity()=>CityState==null?null:new CitySaveState{generated=true,version=CityState.version,seed=CityState.seed,blocks=CityState.blocks,day=CityState.day,housesPerBlock=CityState.housesPerBlock};
    public bool ValidCity(CitySaveState state,int day,bool rewards=false)=>state==null || !state.generated || (state.version>=1 && state.version<=CityGenerator.CurrentVersion) && (state.version<4 || state.housesPerBlock==citySettings.HousesFor(state.version,state.housesPerBlock)) && state.day==day && (state.blocks==citySettings.BlocksForVersion(day,state.version) || rewards && state.version>=7 && state.blocks>=citySettings.stageBlocks[0] && state.blocks<=citySettings.stageBlocks.Last());
    public void RestoreCity(CitySaveState state)
    {
        CityState=state==null || !state.generated?null:new CitySaveState{generated=true,version=state.version,seed=state.seed,blocks=state.blocks,day=state.day,housesPerBlock=state.housesPerBlock};
        if(upgradeRestoredCityLayout && CityState!=null && CityState.version<CityGenerator.CurrentVersion)
        {
            // Saves contain preparation state only: no active route or order depends on old roads.
            CityState.version=CityGenerator.CurrentVersion;
            CityState.blocks=citySettings.Blocks(CityState.day);
            // The roads are new as well, so the old density does not carry over.
            CityState.housesPerBlock=citySettings.HousesFor(CityState.version,0);
        }
        else ExpandRestoredCityHomes();
        PrepareCity(state?.day??1);
    }
    private void ExpandRestoredCityHomes()
    {
        // Versions 1 and 2 use a different expansion schedule and stay untouched.
        // A v4 town already records its chosen density, so do not retune it on load.
        if(!expandExistingCityHomes || CityState==null || CityState.version!=3 || citySettings.housesPerBlock<=2)return;
        GeneratedCity previous=null,expanded=null;
        try
        {
            previous=CityGenerator.Generate(CityState.seed,CityState.blocks,citySettings,version:3);
            expanded=CityGenerator.Generate(CityState.seed,CityState.blocks,citySettings,4,citySettings.housesPerBlock);
            bool sameNodes=previous.map.Nodes.All(node=>expanded.map.Nodes.Any(other=>
                node.Id==other.Id && node.Position==other.Position && node.NodeType==other.NodeType && node.IsStore==other.IsStore));
            bool sameRoads=previous.map.Edges.All(road=>expanded.map.Edges.Any(other=>
                road.Id==other.Id && road.NodeAId==other.NodeAId && road.NodeBId==other.NodeBId &&
                road.Distance==other.Distance && road.RoadType==other.RoadType &&
                road.TrafficFactor==other.TrafficFactor && road.Bidirectional==other.Bidirectional));
            if(!sameNodes || !sameRoads || expanded.metrics.failures.Count!=0)
            {
                Debug.LogWarning("Existing town density was kept because adding houses would alter its roads or fail map validation.");
                return;
            }
            CityState.version=4;
            CityState.housesPerBlock=Mathf.Clamp(citySettings.housesPerBlock,2,4);
        }
        finally
        {
            if(previous!=null)Destroy(previous.map);
            if(expanded!=null)Destroy(expanded.map);
        }
    }
    public void PrepareCity(int day)
    {
        if(!IsReady)return;
        if(UseSampleForValidation || CityState==null)
        {
            if(City!=null){var previousCity=City;City=null;LoadMap(null);Destroy(previousCity.map);}return;
        }
        var campaign=FindFirstObjectByType<CampaignManager>();
        int blocks=BlocksOverride??(CityState.version>=7 && campaign!=null?Mathf.Max(citySettings.BlocksForVersion(day,CityState.version),campaign.Rewards.UnlockedDistricts):citySettings.BlocksForVersion(day,CityState.version));
        if(City!=null && CityState.blocks==blocks && City.seed==CityState.seed && City.day==day && City.blocks.Count==blocks && City.version==CityState.version && City.housesPerBlock==(CityState.version<4?2:CityState.housesPerBlock))return;
        var old=City;var next=CityGenerator.Generate(CityState.seed,blocks,citySettings,CityState.version,CityState.housesPerBlock);
        if(day>=citySettings.constructionStartDay)
        {
            var candidates=next.map.Edges.Where(e=>(next.version>=7?e.Id.Contains("_Street"):e.Id.EndsWith("AB")) && !next.bridges.Contains(e.Id)).ToArray();
            if(candidates.Length>0)
            {
                var e=candidates[(int)((uint)(CityState.seed^(day/citySettings.constructionDays))%(uint)candidates.Length)];
                float before=e.TrafficFactor;
                e.TrafficFactor=citySettings.constructionTraffic;
                var metrics=CityValidator.Measure(next,citySettings);
                if(metrics.failures.Count==0)
                {
                    next.construction.Add(e.Id);
                    next.metrics=metrics;
                }else e.TrafficFactor=before;
            }
        }
        if(next.version>=9)Congest(next,day);
        next.day=day;City=next;CityState.day=day;CityState.blocks=blocks;LoadMap(next.map);if(old!=null)Destroy(old.map);
    }
    /// <summary>v9: every day a share of the main streets is jammed (chosen from the seed and the day), so the plain
    /// shortest route is not always the fastest. Roads whose closure would cut the town, and roads at the shop, are never jammed.</summary>
    private void Congest(GeneratedCity city,int day)
    {
        var cut=CityValidator.CutRoads(city.map);
        // Roads leaving the shop stay clear: a jam there would slow every trip alike.
        string store=city.map.Nodes.First(n=>n.IsStore).Id;
        var streets=city.map.Edges.Where(e=>e.RoadType==RoadType.Main && e.TrafficFactor>=1 && !cut.Contains(e.Id) && !city.construction.Contains(e.Id) && e.NodeAId!=store && e.NodeBId!=store).ToList();
        int count=Mathf.RoundToInt(streets.Count*citySettings.dailyCongestionShare);
        uint Hash(string text){uint h=2166136261;foreach(char c in text)h=unchecked((h^c)*16777619);return h;}
        foreach(var e in streets.OrderBy(e=>Hash(CityState.seed+"/"+day+"/"+e.Id)).Take(count))
            e.TrafficFactor=Mathf.Lerp(citySettings.congestionTrafficMin,citySettings.congestionTrafficMax,Hash(e.Id+"/"+day)%1000/999f);
    }
    public void LoadMap(MapDefinition definition)
    {
        IsReady=false;
        foreach(var view in nodeViews.Values)
        {
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        foreach(var view in edgeViews.Values)
        {
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
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
            if(!renderChunks.TryGetValue(key,out var chunk))
            {
                chunk=new RenderChunk{bounds=renderer.bounds};
                renderChunks.Add(key,chunk);
            }else chunk.bounds.Encapsulate(renderer.bounds);
            chunk.renderers.Add(renderer);
        }
    }
    private void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        foreach(var chunk in renderChunks.Values)
        {
            bool visible=GeometryUtility.TestPlanesAABB(planes,chunk.bounds);
            foreach(var renderer in chunk.renderers)if(renderer!=null && renderer.enabled!=visible)renderer.enabled=visible;
        }
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
        foreach(var edge in edges)
        {
            var view=Instantiate(edgeViewPrefab,edgesParent);
            view.Initialize(edge,nodeLookup[edge.NodeAId],nodeLookup[edge.NodeBId],this,false);
            edgeViews.Add(edge.Id,view);
        }
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
