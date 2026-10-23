using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Isolated calculations and clock checks; never alters the current day's orders.</summary>
public static class M4Validation
{
    [MenuItem("Tools/Pizza Delivery/Validate M4 (Play Mode)")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static int LastCheckCount { get; private set; }

    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        var map = UnityEngine.Object.FindFirstObjectByType<MapManager>();
        if (!Application.isPlaying || map == null || !map.IsReady)
            throw new InvalidOperationException("Enter Play in the Game scene first.");
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("M4: " + name); checks++; }
        void Near(float a, float b, string name) => Check(Mathf.Abs(a-b)<0.001f, name + $" ({a}, expected {b})");
        Near(UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().CreateOrder("Goal1",10).Deadline,10+UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().PromiseSeconds,"Deadline snapshots delivery promise");
        var order = M7Validation.ReadyOrder("Goal1","Test",10,60);
        Near(order.SpawnedAt,10,"Spawn timestamp"); Near(order.Deadline,70f,"Absolute deadline");
        Check(order.Status == OrderStatus.Pending,"New order pending");
        Check(DeliveryScoring.Calculate(50,60,100,5)==100,"Early full score");
        Check(DeliveryScoring.Calculate(60,60,100,5)==100,"Deadline inclusive");
        Check(DeliveryScoring.Calculate(72,60,100,5)==40,"12 seconds late");
        Check(DeliveryScoring.Calculate(80,60,100,5)==0,"20 seconds late");
        Check(DeliveryScoring.Calculate(90,60,100,5)==0,"Never negative");
        Check(DeliveryScoring.Calculate(60.5f,60,100,5)==98,"Fractional score rounding");
        Near(DeliveryScoring.CancellationAt(60,100,5,20),80,"Pending cancellation includes configurable grace");
        Near(DeliveryScoring.CancellationAt(60,100,0,20),80,"Grace expiry is independent of scoring penalty");
        Near(DeliveryScoring.CancellationAt(60,100,5,-1),60,"Negative grace is clamped to zero");

        var person = new DeliveryPerson("Test",1);
        var orders = new[] { order, M7Validation.ReadyOrder("Goal2","Test2",10,60) };
        var route = new ManualDeliveryRoute(map);
        route.TryReplace(new[] {"Store-N1","N1-N2-Main","N2-Goal1","Goal1-Goal2","N2-Goal2","N1-N2-Main","Store-N1"});
        var plan = route.BuildCompletion(orders,person);
        var trip = new DeliveryTrip(map,plan,person,20,5);
        Near(trip.ReturnsAt,20+plan.TotalTime*5,"Return duration conversion");
        Check(trip.Deliveries.Count==2 && trip.Deliveries[0].Order==order,"Arrival ordering");
        Check(trip.Deliveries[0].ArrivesAt<trip.Deliveries[1].ArrivesAt,"Second destination later");
        Check(trip.LastDeliveryAt<trip.ReturnsAt,"Scoring precedes return");
        Check(trip.StatusAt(20)==DeliveryStatus.Delivering,"Initial delivering state");
        Check(trip.StatusAt(trip.LastDeliveryAt)==DeliveryStatus.Returning,"Returning after final drop");
        Check(trip.StatusAt(trip.ReturnsAt)==DeliveryStatus.Idle,"Idle at return boundary");
        var store=map.Nodes.First(n=>n.IsStore).Position;
        Near(Vector2.Distance(trip.PositionAt(20),store),0,"Marker starts at store");
        Near(Vector2.Distance(trip.PositionAt(trip.ReturnsAt+1),store),0,"Marker ends at store");
        Check(Vector2.Distance(trip.PositionAt(23),store)>0,"Marker moves between boundaries");
        float returnsAt=trip.ReturnsAt; person.SkillLevel=0.3f; route.Clear();
        Near(trip.ReturnsAt,returnsAt,"Dispatch timing is frozen");
        Check(trip.EdgeIds.Count==7,"Editing next route leaves dispatched route intact");
        var duplicateDestination = new[] {order,M7Validation.ReadyOrder("Goal1","Same address",10,60)};
        route.TryReplace(new[] {"Store-N1","N1-N2-Main","N2-Goal1","N2-Goal1","N1-N2-Main","Store-N1"});
        var same = new DeliveryTrip(map,route.BuildCompletion(duplicateDestination,person),person,0,5);
        Near(same.Deliveries[0].ArrivesAt,same.Deliveries[1].ArrivesAt,"Two orders at same address arrive together");

        var fixture=new GameObject("M4 isolated clock validation");
        fixture.SetActive(false);
        try
        {
            var clock=fixture.AddComponent<GameClock>();
            var so=new SerializedObject(clock);
            so.FindProperty("businessDuration").floatValue=10;
            so.FindProperty("planningTimeRate").floatValue=0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            int opened=0,closed=0,ended=0;
            clock.OnDayStarted+=()=>opened++; clock.OnClosed+=()=>closed++; clock.OnDayEnded+=()=>ended++;
            clock.BeginDay(); clock.BeginDay(); Check(opened==1,"Day starts once");
            Near(clock.ToRealSeconds(3),15,"Clock conversion");
            clock.IsPlanning=true; clock.Advance(4); Near(clock.ElapsedSeconds,2,"Planning speed");
            clock.IsPlanning=false; clock.Advance(10);
            Check(clock.Phase==DayPhase.Closed && closed==1,"Closing boundary crossed once");
            Near(clock.ElapsedSeconds,12,"Travel clock continues after close");
            Near(clock.RemainingSeconds,0,"Countdown never negative");
            clock.FinishDay(); clock.FinishDay(); Check(ended==1,"Results emitted once");
            clock.Advance(10); Near(clock.ElapsedSeconds,12,"Ended clock freezes");
            bool rejected=false; try {clock.Advance(float.NaN);} catch(ArgumentOutOfRangeException){rejected=true;}
            Check(rejected,"Invalid delta rejected");
            var stats=fixture.AddComponent<DayStats>();
            stats.RecordDelivery(100,0); stats.RecordDelivery(40,12); stats.RecordCancellation(2,3);
            Check(stats.DeliveredCount==2 && stats.LateCount==1 && stats.CancelledCount==2 && stats.TotalScore==134,"Daily aggregation");
        }
        finally { UnityEngine.Object.DestroyImmediate(fixture); }
        LastCheckCount = checks;
        return $"PASS: {checks} M4 deadline, score, trip, clock and statistics checks.";
    }
}
