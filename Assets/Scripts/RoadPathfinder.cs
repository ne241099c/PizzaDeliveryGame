using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RouteSuggestion
{
    public IReadOnlyList<string> EdgeIds { get; }
    public float TotalTime { get; }
    public RouteSuggestion(IEnumerable<string> edges, float totalTime)
    {
        EdgeIds = Array.AsReadOnly(edges.ToArray());
        TotalTime = totalTime;
    }
}

/// <summary>Dijkstra search for the optional suggestion. Manual routes do not depend on it.</summary>
public static class RoadPathfinder
{
    public static List<string> FindShortestPath(IReadOnlyList<MapEdge> edges, string from, string to,
        Func<MapEdge, float> getTime)
    {
        if (from == to) return new List<string>();
        var outgoing = new Dictionary<string, List<MapEdge>>();
        foreach (MapEdge edge in edges)
        {
            if (!outgoing.ContainsKey(edge.NodeAId)) outgoing[edge.NodeAId] = new List<MapEdge>();
            if (!outgoing.ContainsKey(edge.NodeBId)) outgoing[edge.NodeBId] = new List<MapEdge>();
            outgoing[edge.NodeAId].Add(edge);
            if (edge.Bidirectional) outgoing[edge.NodeBId].Add(edge);
        }
        if (!outgoing.ContainsKey(from) || !outgoing.ContainsKey(to)) return null;
        var distance = new Dictionary<string, float> { [from] = 0f };
        var previous = new Dictionary<string, (string node, string edge)>();
        var queue = new SortedSet<(float cost, int serial, string node)>();
        int serial = 0;
        queue.Add((0f, serial++, from));
        while (queue.Count > 0)
        {
            var item = queue.Min;
            queue.Remove(item);
            if (item.cost > distance[item.node]) continue;
            if (item.node == to) break;
            foreach (MapEdge edge in outgoing[item.node])
            {
                if (!edge.TryTraverse(item.node, out string next)) continue;
                float time = getTime(edge);
                if (float.IsNaN(time) || float.IsInfinity(time) || time < 0)
                    throw new ArgumentException("Road times must be finite and non-negative.");
                float candidate = item.cost + time;
                if (distance.TryGetValue(next, out float known) && candidate >= known) continue;
                distance[next] = candidate;
                previous[next] = (item.node, edge.Id);
                queue.Add((candidate, serial++, next));
            }
        }
        if (!distance.ContainsKey(to)) return null;
        var path = new List<string>();
        for (string node = to; node != from;)
        {
            var step = previous[node];
            path.Add(step.edge);
            node = step.node;
        }
        path.Reverse();
        return path;
    }

    public static RouteSuggestion Suggest(MapManager map, IReadOnlyList<Order> orders, DeliveryPerson person)
    {
        if (!map.IsReady || person == null || !person.IsAvailable || orders.Count == 0 || orders.Count > 2
            || orders.Any(o => o == null || !map.HasNode(o.DestinationNodeId))) return null;
        var sequences = new List<string[]> { orders.Select(o => o.DestinationNodeId).ToArray() };
        if (orders.Count == 2) sequences.Add(sequences[0].Reverse().ToArray());
        RouteSuggestion best = null;
        foreach (string[] sequence in sequences)
        {
            string current = map.StoreNodeId;
            var path = new List<string>();
            bool reachable = true;
            foreach (string goal in sequence.Concat(new[] { map.StoreNodeId }))
            {
                List<string> segment = FindShortestPath(map.Edges, current, goal,
                    e => map.GetTravelTime(e.Id, person.SkillLevel));
                if (segment == null) { reachable = false; break; }
                path.AddRange(segment);
                current = goal;
            }
            if (!reachable) continue;
            float total = path.Sum(id => map.GetTravelTime(id, person.SkillLevel));
            if (best == null || total < best.TotalTime) best = new RouteSuggestion(path, total);
        }
        return best;
    }

    /// <summary>Sum of independent store-to-customer round trips, with the same courier and road costs.</summary>
    public static float? GetIndividualReferenceTime(MapManager map, IReadOnlyList<Order> orders, DeliveryPerson person)
    {
        if (!map.IsReady || person == null || !person.IsAvailable || orders.Count == 0
            || orders.Any(o => o == null || !map.HasNode(o.DestinationNodeId))) return null;
        float total = 0f;
        foreach (Order order in orders)
        {
            List<string> outbound = FindShortestPath(map.Edges, map.StoreNodeId, order.DestinationNodeId,
                e => map.GetTravelTime(e.Id, person.SkillLevel));
            List<string> inbound = FindShortestPath(map.Edges, order.DestinationNodeId, map.StoreNodeId,
                e => map.GetTravelTime(e.Id, person.SkillLevel));
            if (outbound == null || inbound == null) return null;
            total += outbound.Concat(inbound).Sum(id => map.GetTravelTime(id, person.SkillLevel));
        }
        return total;
    }
}
