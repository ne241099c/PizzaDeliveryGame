using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ScheduledDelivery
{
    public Order Order { get; }
    public float ArrivesAt { get; }
    public bool Resolved { get; internal set; }
    public ScheduledDelivery(Order order, float arrivesAt) { Order = order; ArrivesAt = arrivesAt; }
}

/// <summary>An immutable timing/geometry snapshot. Later edits never change a dispatched trip.</summary>
public sealed class DeliveryTrip
{
    private readonly Vector2[] starts;
    private readonly Vector2[] ends;
    private readonly float[] segmentEndTimes;
    public DeliveryPerson Person { get; }
    public IReadOnlyList<string> EdgeIds { get; }
    public IReadOnlyList<ScheduledDelivery> Deliveries { get; }
    public float StartedAt { get; }
    public float ReturnsAt { get; }
    public float LastDeliveryAt { get; }
    public bool Returned { get; internal set; }

    public DeliveryTrip(MapManager map, DeliveryPlan plan, DeliveryPerson person, float startedAt, float multiplier)
    {
        if (plan == null || plan.EdgeIds.Count == 0 || plan.Orders.Count == 0 || multiplier <= 0)
            throw new ArgumentException("A valid route, orders, and positive multiplier are required.");
        Person = person;
        StartedAt = startedAt;
        EdgeIds = Array.AsReadOnly(plan.EdgeIds.ToArray());
        starts = new Vector2[EdgeIds.Count];
        ends = new Vector2[EdgeIds.Count];
        segmentEndTimes = new float[EdgeIds.Count];
        var positions = map.Nodes.ToDictionary(n => n.Id, n => n.Position);
        var arrivals = new Dictionary<string, float>();
        string current = map.StoreNodeId;
        float elapsed = 0;
        for (int i = 0; i < EdgeIds.Count; i++)
        {
            MapEdge edge = map.GetEdge(EdgeIds[i]);
            if (!edge.TryTraverse(current, out string next)) throw new ArgumentException("Disconnected dispatched route.");
            starts[i] = positions[current];
            ends[i] = positions[next];
            // Match the backstreet's visible offset, including on the reverse traversal.
            if (edge.RoadType == RoadType.Backstreet)
            {
                Vector2 direction = (positions[edge.NodeBId] - positions[edge.NodeAId]).normalized;
                Vector2 offset = new Vector2(-direction.y, direction.x) * 0.35f;
                starts[i] += offset;
                ends[i] += offset;
            }
            elapsed += map.GetTravelTime(edge.Id, person.SkillLevel) * multiplier;
            segmentEndTimes[i] = startedAt + elapsed;
            if (!arrivals.ContainsKey(next)) arrivals[next] = startedAt + elapsed;
            current = next;
        }
        if (current != map.StoreNodeId || plan.Orders.Any(o => !arrivals.ContainsKey(o.DestinationNodeId)))
            throw new ArgumentException("Route must visit the selected destinations and return to Store.");
        Deliveries = Array.AsReadOnly(plan.Orders.Select(o => new ScheduledDelivery(o, arrivals[o.DestinationNodeId]))
            .OrderBy(d => d.ArrivesAt).ToArray());
        ReturnsAt = startedAt + elapsed;
        LastDeliveryAt = Deliveries.Max(d => d.ArrivesAt);
    }

    public DeliveryStatus StatusAt(float now) => now >= ReturnsAt ? DeliveryStatus.Idle
        : now >= LastDeliveryAt ? DeliveryStatus.Returning : DeliveryStatus.Delivering;

    public Vector2 PositionAt(float now)
    {
        for (int i = 0; i < segmentEndTimes.Length; i++)
        {
            if (now > segmentEndTimes[i]) continue;
            float begin = i == 0 ? StartedAt : segmentEndTimes[i - 1];
            return Vector2.Lerp(starts[i], ends[i], Mathf.InverseLerp(begin, segmentEndTimes[i], now));
        }
        return ends[ends.Length - 1];
    }
}
