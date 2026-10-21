using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class OrderSpawner : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private MapManager map;
    [SerializeField] private OrderListUI orders;
    [SerializeField, Min(0.1f)] private float interval = 25f;
    [SerializeField, Range(0f, 0.9f)] private float intervalVariation = 0.3f;
    [SerializeField, Min(0.01f)] private float deadlineFactor = 1.8f;
    private float nextSpawn;
    private bool scheduled;

    private void OnEnable()
    {
        clock.OnDayStarted += ScheduleFirst;
        clock.Advanced += Tick;
        map.MapReady += MapReady;
    }
    private void OnDisable()
    {
        clock.OnDayStarted -= ScheduleFirst;
        clock.Advanced -= Tick;
        map.MapReady -= MapReady;
    }
    private void Start() { if (clock.Phase == DayPhase.Open) ScheduleFirst(); }
    private void MapReady() { if (clock.Phase == DayPhase.Open && !scheduled) ScheduleFirst(); }
    private float NextInterval() => Mathf.Max(0.1f, interval * Random.Range(1f - intervalVariation, 1f + intervalVariation));
    private void ScheduleFirst()
    {
        if (scheduled || !map.IsReady) return;
        nextSpawn = clock.ElapsedSeconds + NextInterval();
        scheduled = true;
    }
    private void Tick(float before, float now)
    {
        if (!scheduled || clock.Phase != DayPhase.Open || !map.IsReady) return;
        var destinations = map.Nodes.Where(n => n.NodeType == NodeType.DeliveryAddress && !n.IsStore).ToArray();
        if (destinations.Length == 0) return;
        while (nextSpawn <= now && nextSpawn < clock.BusinessDuration)
        {
            MapNode destination = destinations[Random.Range(0, destinations.Length)];
            var probe = new Order(destination.Id, destination.Id, nextSpawn, 0);
            float? mapTime = RoadPathfinder.GetIndividualReferenceTime(map, new[] { probe }, new DeliveryPerson("Reference", 1f));
            if (mapTime.HasValue)
                orders.AddOrder(new Order(destination.Id, "Delivery to " + destination.Id, nextSpawn,
                    CalculateDeadlineDuration(mapTime.Value, clock.TravelTimeMultiplier, deadlineFactor)));
            nextSpawn += NextInterval();
        }
    }
    public static float CalculateDeadlineDuration(float roundTripTime, float multiplier, float factor)
        => roundTripTime * multiplier * factor;
}
