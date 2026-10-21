using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MapManager : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float baseSpeed = 10f;
    [SerializeField, Min(0f)] private float backstreetPenalty = 1f;
    [SerializeField, Min(0.01f)] private float drivingSpeedBase = 0.7f;
    [SerializeField, Min(0f)] private float drivingSpeedScale = 0.6f;
    [SerializeField] private NodeView nodeViewPrefab;
    [SerializeField] private EdgeView edgeViewPrefab;
    [SerializeField] private Transform nodesParent;
    [SerializeField] private Transform edgesParent;
    [SerializeField] private MapDefinition mapDefinition;

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
    public string StoreNodeId { get; private set; }
    public IReadOnlyList<MapNode> Nodes => nodes;
    public IReadOnlyList<MapEdge> Edges => edges;

    private void Start()
    {
        CreateMap();
        ValidateAndIndex();
        foreach (MapNode node in nodes)
        {
            NodeView view = Instantiate(nodeViewPrefab, nodesParent);
            view.Initialize(node);
            nodeViews.Add(node.Id, view);
        }
        foreach (MapEdge edge in edges)
        {
            EdgeView view = Instantiate(edgeViewPrefab, edgesParent);
            view.Initialize(edge, nodeLookup[edge.NodeAId], nodeLookup[edge.NodeBId], this, false);
            edgeViews.Add(edge.Id, view);
        }
        IsReady = true;
        PreviewRoute(Array.Empty<string>(), StoreNodeId, 1f);
        MapReady?.Invoke();
    }

    private void Update()
    {
        if (!IsReady || Mouse.current == null) return;
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

    public float GetTravelTime(string edgeId, DeliveryPerson person)
        => GetTravelTime(edgeId, person.LocalKnowledge) / GetDrivingSpeedFactor(person);

    public float GetTravelTime(string edgeId, float skillLevel = 1f)
    {
        MapEdge edge = GetEdge(edgeId);
        float time = edge.Distance / (baseSpeed * edge.TrafficFactor);
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
        edges.Add(new MapEdge("N1-N2-Backstreet", "N1", "N2", 13f, RoadType.Backstreet, 1f));
        edges.Add(new MapEdge("N2-Goal1", "N2", "Goal1", 5f, RoadType.Main, 1f));
        edges.Add(new MapEdge("N2-Goal2", "N2", "Goal2", 7f, RoadType.Main, 1f));
        edges.Add(new MapEdge("Goal1-Goal2", "Goal1", "Goal2", 3f, RoadType.Main, 1f));
    }
}
