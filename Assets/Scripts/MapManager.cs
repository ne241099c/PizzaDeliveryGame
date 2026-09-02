using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [SerializeField] private float baseSpeed = 10f;
    [SerializeField] private NodeView nodeViewPrefab;
    [SerializeField] private EdgeView edgeViewPrefab;
    [SerializeField] private Transform nodesParent;
    [SerializeField] private Transform edgesParent;

    private const string EdgeStoreToN1 = "Store-N1";
    private const string EdgeN1ToN2Main = "N1-N2-Main";
    private const string EdgeN1ToN2Backstreet = "N1-N2-Backstreet";
    private const string EdgeN2ToGoal = "N2-Goal";

    private readonly List<MapNode> nodes = new();
    private readonly List<MapEdge> edges = new();
    private readonly RouteState routeState = new();
    private readonly Dictionary<string, EdgeView> edgeViews = new();

    public event Action<RouteState> OnRouteChanged;
    public RouteState CurrentRouteState => routeState;

    private void Start()
    {
        CreateTestMap();
        SetMainRoute();
        RecalculateRoute();
        CreateMapViews();
        UpdateEdgeViews();

        OnRouteChanged?.Invoke(routeState);
    }

    public void ToggleEdge(string edgeId)
    {
        if (edgeId != EdgeN1ToN2Backstreet)
        {
            return;
        }

        bool usesBackstreet = routeState.ActiveEdgeIds.Contains(EdgeN1ToN2Backstreet);

        if (usesBackstreet)
        {
            routeState.ActiveEdgeIds.Remove(EdgeN1ToN2Backstreet);
            routeState.ActiveEdgeIds.Add(EdgeN1ToN2Main);
        }
        else
        {
            routeState.ActiveEdgeIds.Remove(EdgeN1ToN2Main);
            routeState.ActiveEdgeIds.Add(EdgeN1ToN2Backstreet);
        }

        RecalculateRoute();
        UpdateEdgeViews();

        OnRouteChanged?.Invoke(routeState);
    }

    private void CreateTestMap()
    {
        nodes.Clear();
        edges.Clear();

        nodes.Add(new MapNode("Store", new Vector2(-4f, 0f), NodeType.Intersection, true));
        nodes.Add(new MapNode("N1", new Vector2(-1.5f, 0f), NodeType.Intersection, false));
        nodes.Add(new MapNode("N2", new Vector2(1.5f, 0f), NodeType.Intersection, false));
        nodes.Add(new MapNode("Goal", new Vector2(4f, -1f), NodeType.DeliveryAddress, false));

        edges.Add(new MapEdge(EdgeStoreToN1, "Store", "N1", 10f, RoadType.Main, 0.8f));
        edges.Add(new MapEdge(EdgeN1ToN2Main, "N1", "N2", 10f, RoadType.Main, 0.6f));
        edges.Add(new MapEdge(EdgeN1ToN2Backstreet, "N1", "N2", 13f, RoadType.Backstreet, 1.0f));
        edges.Add(new MapEdge(EdgeN2ToGoal, "N2", "Goal", 5f, RoadType.Main, 1.0f));
    }

    private void SetMainRoute()
    {
        routeState.ActiveEdgeIds.Clear();
        routeState.ActiveEdgeIds.Add(EdgeStoreToN1);
        routeState.ActiveEdgeIds.Add(EdgeN1ToN2Main);
        routeState.ActiveEdgeIds.Add(EdgeN2ToGoal);
    }

    private void CreateMapViews()
    {
        foreach (MapNode node in nodes)
        {
            NodeView nodeView = Instantiate(nodeViewPrefab, nodesParent);
            nodeView.Initialize(node);
        }

        foreach (MapEdge edge in edges)
        {
            MapNode nodeA = FindNode(edge.NodeAId);
            MapNode nodeB = FindNode(edge.NodeBId);
            bool isActive = routeState.ActiveEdgeIds.Contains(edge.Id);

            EdgeView edgeView = Instantiate(edgeViewPrefab, edgesParent);
            edgeView.Initialize(edge, nodeA, nodeB, this, isActive);

            edgeViews.Add(edge.Id, edgeView);
        }
    }

    private void UpdateEdgeViews()
    {
        foreach (MapEdge edge in edges)
        {
            bool isActive = routeState.ActiveEdgeIds.Contains(edge.Id);
            edgeViews[edge.Id].SetActive(isActive);
        }
    }

    private void RecalculateRoute()
    {
        float totalDistance = 0f;
        float estimatedTime = 0f;

        foreach (string edgeId in routeState.ActiveEdgeIds)
        {
            MapEdge edge = edges.First(e => e.Id == edgeId);

            totalDistance += edge.Distance;
            estimatedTime += CalculateEdgeTime(edge);
        }

        routeState.TotalDistance = totalDistance;
        routeState.EstimatedTime = estimatedTime;
    }

    private float CalculateEdgeTime(MapEdge edge)
    {
        return edge.Distance / (baseSpeed * edge.TrafficFactor);
    }

    private MapNode FindNode(string nodeId)
    {
        return nodes.First(node => node.Id == nodeId);
    }
}
