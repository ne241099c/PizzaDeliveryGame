using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ScheduledDelivery
{
    public Order Order { get; }
    public float ArrivesAt { get; internal set; }
    public bool Resolved { get; internal set; }
    public ScheduledDelivery(Order order, float arrivesAt) { Order = order; ArrivesAt = arrivesAt; }
}

/// <summary>Frozen route and speed; actual arrival times can be delayed by accidents.</summary>
public sealed class DeliveryTrip
{
    private readonly Vector2[] starts;
    private readonly Vector2[] ends;
    private readonly float[] segmentEndTimes;
    private readonly float[] segmentStartTimes;
    private readonly float[] stopSeconds;
    private readonly MapEdge[] roads;
    private readonly int[] deliverySegments;
    private readonly DeliveryDynamics dynamics;
    private readonly Func<double> roll;
    private int nextEdgeToCheck;
    public int BackstreetCount=>roads.Count(r=>r.RoadType==RoadType.Backstreet);
    public int SafeRoadCount=>roads.Length-AccidentCount;
    public float EquipmentRiskAtDispatch {get;}
    public float StartingStamina { get; }
    public float EnduranceAtDispatch { get; }
    public float SafetyAtDispatch { get; }
    public int AccidentCount { get; private set; }
    public float TotalDelaySeconds { get; private set; }
    public DeliveryPerson Person { get; }
    public IReadOnlyList<string> EdgeIds { get; }
    public IReadOnlyList<ScheduledDelivery> Deliveries { get; }
    public float ServiceAtDispatch { get; }
    public float StartedAt { get; }
    public float ReturnsAt => segmentEndTimes[segmentEndTimes.Length-1];
    public float LastDeliveryAt => Deliveries.Max(d=>d.ArrivesAt);
    public bool Returned { get; internal set; }

    public DeliveryTrip(MapManager map, DeliveryPlan plan, DeliveryPerson person, float startedAt, float multiplier, DeliveryDynamics dynamics = null, Func<double> roll = null)
    {
        if (plan == null || plan.EdgeIds.Count == 0 || plan.Orders.Count == 0 || multiplier <= 0)
            throw new ArgumentException("A valid route, orders, and positive multiplier are required.");
        this.dynamics=dynamics;EquipmentRiskAtDispatch=dynamics!=null?dynamics.EquipmentRiskMultiplier:1;
        this.roll=roll ?? dynamics?.CreateRoller();
        StartingStamina=person.CurrentStamina;
        EnduranceAtDispatch=person.Endurance;
        SafetyAtDispatch=person.Safety;
        Person = person;
        ServiceAtDispatch = person.Service;
        StartedAt = startedAt;
        EdgeIds = Array.AsReadOnly(plan.EdgeIds.ToArray());
        starts = new Vector2[EdgeIds.Count];
        ends = new Vector2[EdgeIds.Count];
        segmentEndTimes = new float[EdgeIds.Count];
        segmentStartTimes = new float[EdgeIds.Count];
        stopSeconds = new float[EdgeIds.Count];
        roads = new MapEdge[EdgeIds.Count];
        var arrivalSegments = new Dictionary<string,int>();
        var positions = map.Nodes.ToDictionary(n => n.Id, n => n.Position);
        var arrivals = new Dictionary<string, float>();
        string current = map.StoreNodeId;
        float elapsed = 0;
        for (int i = 0; i < EdgeIds.Count; i++)
        {
            MapEdge edge = map.GetEdge(EdgeIds[i]);
            roads[i]=edge;
            segmentStartTimes[i]=startedAt+elapsed;
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
            elapsed += map.GetTravelTime(edge.Id, person) * multiplier;
            segmentEndTimes[i] = startedAt + elapsed;
            if (!arrivals.ContainsKey(next)) {arrivals[next] = startedAt + elapsed;arrivalSegments[next]=i;}
            current = next;
        }
        if (current != map.StoreNodeId || plan.Orders.Any(o => !arrivals.ContainsKey(o.DestinationNodeId)))
            throw new ArgumentException("Route must visit the selected destinations and return to Store.");
        Deliveries = Array.AsReadOnly(plan.Orders.Select(o => new ScheduledDelivery(o, arrivals[o.DestinationNodeId]))
            .OrderBy(d => d.ArrivesAt).ToArray());
        deliverySegments=Deliveries.Select(d=>arrivalSegments[d.Order.DestinationNodeId]).ToArray();
    }

    public DeliveryStatus StatusAt(float now) => now >= ReturnsAt ? DeliveryStatus.Idle
        : now >= LastDeliveryAt ? DeliveryStatus.Returning : DeliveryStatus.Delivering;

    // Called before arrival scoring. Draw once on entry to each actual road traversal.
    public void AdvanceAccidents(float now, Action<string,float> onAccident = null)
    {
        if(dynamics==null || roll==null)return;
        while(nextEdgeToCheck<roads.Length && segmentStartTimes[nextEdgeToCheck]<=now)
        {
            int index=nextEdgeToCheck++;
            float probability=Mathf.Clamp01(dynamics.AccidentProbability(SafetyAtDispatch,StartingStamina,roads[index].RoadType)*EquipmentRiskAtDispatch);
            if(roll()>=probability)continue;
            float delay=dynamics.AccidentStopSeconds;
            stopSeconds[index]+=delay;
            for(int i=index;i<roads.Length;i++)
            {
                segmentEndTimes[i]+=delay;
                if(i>index)segmentStartTimes[i]+=delay;
            }
            for(int i=0;i<Deliveries.Count;i++)if(deliverySegments[i]>=index)Deliveries[i].ArrivesAt+=delay;
            AccidentCount++;TotalDelaySeconds+=delay;
            onAccident?.Invoke(roads[index].Id,delay);
        }
    }
    // Uses road distances, not visual coordinates or elapsed stopped time.
    public float DistanceAt(float now)
    {
        float distance=0;
        for(int i=0;i<roads.Length;i++)
        {
            float movingFrom=segmentStartTimes[i]+stopSeconds[i];
            if(now<=movingFrom)continue;
            float fraction=now>=segmentEndTimes[i] ? 1 : Mathf.InverseLerp(movingFrom,segmentEndTimes[i],now);
            distance+=roads[i].Distance*fraction;
        }
        return distance;
    }
    public float StoppedUntil(float now)
    {
        for(int i=0;i<stopSeconds.Length;i++)
            if(now>=segmentStartTimes[i] && now<segmentStartTimes[i]+stopSeconds[i])return segmentStartTimes[i]+stopSeconds[i];
        return now;
    }
    public bool IsStoppedAt(float now) => StoppedUntil(now)>now;
    public Vector2 PositionAt(float now)
    {
        for (int i = 0; i < segmentEndTimes.Length; i++)
        {
            if (now > segmentEndTimes[i]) continue;
            float movingFrom=segmentStartTimes[i]+stopSeconds[i];
            return now<=movingFrom ? starts[i]
                : Vector2.Lerp(starts[i],ends[i],Mathf.InverseLerp(movingFrom,segmentEndTimes[i],now));
        }
        return ends[ends.Length - 1];
    }
}
