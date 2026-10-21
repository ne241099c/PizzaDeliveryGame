using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>An ordered walk through a graph; repeated roads and intermediate store visits are allowed.</summary>
public sealed class ManualDeliveryRoute
{
    private readonly MapManager map;
    private readonly List<string> edgeIds = new();
    private readonly List<string> nodeIds = new();
    public IReadOnlyList<string> EdgeIds => edgeIds;
    public IReadOnlyList<string> NodeIds => nodeIds;
    public string CurrentNodeId => nodeIds[nodeIds.Count - 1];

    public ManualDeliveryRoute(MapManager map)
    {
        if (!map.IsReady) throw new InvalidOperationException("The map is not ready.");
        this.map = map;
        nodeIds.Add(map.StoreNodeId);
    }

    public bool TryAppend(string edgeId, out string message)
    {
        MapEdge edge;
        try { edge = map.GetEdge(edgeId); }
        catch (ArgumentException) { message = "Unknown road."; return false; }
        if (!edge.TryTraverse(CurrentNodeId, out string next))
        {
            message = "Choose a road connected to " + CurrentNodeId + ".";
            return false;
        }
        edgeIds.Add(edgeId);
        nodeIds.Add(next);
        message = "";
        return true;
    }

    public bool Undo()
    {
        if (edgeIds.Count == 0) return false;
        edgeIds.RemoveAt(edgeIds.Count - 1);
        nodeIds.RemoveAt(nodeIds.Count - 1);
        return true;
    }

    public void Clear()
    {
        edgeIds.Clear();
        nodeIds.Clear();
        nodeIds.Add(map.StoreNodeId);
    }

    // Validate the whole suggestion before replacing the player's existing route.
    public bool TryReplace(IEnumerable<string> suggestedEdges)
    {
        var replacement = new ManualDeliveryRoute(map);
        foreach (string edge in suggestedEdges)
            if (!replacement.TryAppend(edge, out _)) return false;
        edgeIds.Clear();
        edgeIds.AddRange(replacement.edgeIds);
        nodeIds.Clear();
        nodeIds.AddRange(replacement.nodeIds);
        return true;
    }

    public float GetTime(float skillLevel) => edgeIds.Sum(id => map.GetTravelTime(id, skillLevel));
    public float GetTime(DeliveryPerson person) => edgeIds.Sum(id => map.GetTravelTime(id, person));
    public bool HasVisited(string nodeId) => nodeIds.Contains(nodeId);

    public DeliveryPlan BuildCompletion(IReadOnlyList<Order> orders, DeliveryPerson person)
    {
        if (person == null || !person.IsAvailable || orders.Count == 0 || edgeIds.Count == 0
            || CurrentNodeId != map.StoreNodeId || orders.Distinct().Count() != orders.Count
            || orders.Any(o => o == null || !HasVisited(o.DestinationNodeId))) return null;
        int firstDelivery = orders.Min(o => nodeIds.IndexOf(o.DestinationNodeId));
        int lastDelivery = orders.Max(o => nodeIds.IndexOf(o.DestinationNodeId));
        bool isBatch = orders.Count > 1
            && !nodeIds.Skip(firstDelivery).Take(lastDelivery - firstDelivery + 1).Contains(map.StoreNodeId);
        string title = orders.Count == 1 ? "Single delivery" : isBatch ? "Combined route" : "Individual route";
        return new DeliveryPlan(title, string.Join(" > ", nodeIds), GetTime(person),
            isBatch, orders, edgeIds);
    }
}
