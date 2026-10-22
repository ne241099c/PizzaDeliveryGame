using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Checks the sample graph, manually drawn routes, and optional path suggestion.</summary>
public static partial class M3Validation
{
    [MenuItem("Tools/Pizza Delivery/Validate M3 Sample (Play Mode)")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        MapManager map = UnityEngine.Object.FindFirstObjectByType<MapManager>();
        if (!Application.isPlaying || map == null || !map.IsReady)
            throw new InvalidOperationException("Open SampleScene and enter Play Mode first.");
        int checks = 0;
        Action<bool, string> check = (ok, name) =>
        {
            if (!ok) throw new Exception("M3 validation failed: " + name);
            checks++;
        };
        Action<float, float, string> near = (actual, expected, name) =>
            check(Mathf.Abs(actual - expected/map.DeliverySpeedMultiplier) < 0.001f, $"{name}: actual {actual}, expected {expected}");
        var expert = new DeliveryPerson("Expert", 1f);
        var rookie = new DeliveryPerson("Ren", 0.3f);
        var orders = new[] { M7Validation.ReadyOrder("Goal1", "Test1", 0, 60), M7Validation.ReadyOrder("Goal2", "Test2", 0, 60) };
        var route = new ManualDeliveryRoute(map);
        check(route.CurrentNodeId == map.StoreNodeId && route.EdgeIds.Count == 0, "Starts at Store");
        check(!route.TryAppend("Goal1-Goal2", out _), "Disconnected road rejected");
        check(!route.TryAppend("missing", out _), "Unknown road rejected");
        check(route.TryAppend("Store-N1", out _), "Append connected road");
        check(route.CurrentNodeId == "N1", "Current node follows road");
        check(route.TryAppend("Store-N1", out _), "Reverse traversal allowed");
        near(route.GetTime(1f), 2.5f, "Repeated road counted twice");
        check(route.Undo() && route.CurrentNodeId == "N1", "Undo restores position");
        check(!route.TryReplace(new[] { "Store-N1", "Goal1-Goal2" }) && route.CurrentNodeId == "N1", "Invalid replacement is atomic");
        route.Clear();
        check(route.EdgeIds.Count == 0 && !route.Undo(), "Clear and empty undo");

        string[] full = { "Store-N1", "N1-N2-Main", "N2-Goal1", "Goal1-Goal2", "N2-Goal2", "N1-N2-Main", "Store-N1" };
        check(route.TryReplace(full), "Manual route accepted");
        near(route.GetTime(1f), 7.333333f, "Main-road manual round trip");
        check(route.BuildCompletion(orders, expert)?.Orders.Count == 2, "All selected destinations and return required");
        check(route.BuildCompletion(orders, expert).IsBatch, "Two deliveries before returning are combined");
        check(route.BuildCompletion(orders, null) == null, "No courier rejected");
        check(route.BuildCompletion(Array.Empty<Order>(), expert) == null, "No orders rejected");
        check(route.BuildCompletion(new[] { orders[0], orders[0] }, expert) == null, "Duplicate order rejected");
        route.Undo();
        check(route.BuildCompletion(orders, expert) == null, "Must return to Store");
        route.TryReplace(new[] { "Store-N1", "N1-N2-Main", "N2-Goal1", "N2-Goal1", "N1-N2-Main", "Store-N1" });
        check(route.BuildCompletion(orders, expert) == null, "Missing selected destination rejected");
        check(route.BuildCompletion(new[] { orders[0] }, expert) != null, "Single order complete");
        route.TryReplace(new[] { "Store-N1", "N1-N2-Main", "N2-Goal1", "Goal1-Goal2", "Goal1-Goal2", "N2-Goal1", "N1-N2-Main", "Store-N1" });
        near(route.GetTime(1f), 7.433333f, "Return via a delivered address");
        check(route.BuildCompletion(orders, expert) != null, "Revisiting delivered address is allowed");
        route.TryReplace(full.Select(id => id == "N1-N2-Main" ? "N1-N2-Backstreet" : id));
        near(route.GetTime(1f), 6.2f, "Manual backstreet route");
        near(route.GetTime(0.3f), 7.74f, "Skill applied to every traversal");
        near(RoadPathfinder.Suggest(map, orders, expert).TotalTime, 6.2f, "Expert suggestion uses backstreet");
        near(RoadPathfinder.Suggest(map, orders, rookie).TotalTime, 7.333333f, "Rookie suggestion uses main road");
        near(RoadPathfinder.GetIndividualReferenceTime(map, orders, expert).Value, 11.8f, "Individual reference uses expert's optimal roads");
        near(RoadPathfinder.GetIndividualReferenceTime(map, orders, rookie).Value, 14.066667f, "Individual reference uses rookie's optimal roads");
        near(RoadPathfinder.GetIndividualReferenceTime(map, new[] { orders[0] }, expert).Value, 5.7f, "Single-order reference is one full round trip");
        check(!RoadPathfinder.GetIndividualReferenceTime(map, orders, null).HasValue, "No reference without courier");
        check(!RoadPathfinder.GetIndividualReferenceTime(map, Array.Empty<Order>(), expert).HasValue, "No reference without orders");
        near(RoadPathfinder.GetIndividualReferenceTime(map, orders, expert).Value
            - RoadPathfinder.Suggest(map, orders, expert).TotalTime, 5.6f, "Combined saving versus individual reference");
        string[] separate = { "Store-N1", "N1-N2-Main", "N2-Goal1", "N2-Goal1", "N1-N2-Main", "Store-N1",
            "Store-N1", "N1-N2-Main", "N2-Goal2", "N2-Goal2", "N1-N2-Main", "Store-N1" };
        route.TryReplace(separate);
        check(!route.BuildCompletion(orders, expert).IsBatch, "Store visit between deliveries is individual");
        near(route.GetTime(expert.SkillLevel), 14.066667f, "Manually drawn individual trips count both returns");
        route.TryReplace(new[] { "Store-N1", "Store-N1" }.Concat(full));
        check(route.BuildCompletion(orders, expert).IsBatch, "Empty initial loop does not change delivery classification");
        check(RoadPathfinder.Suggest(map, orders, null) == null, "No suggestion without courier");
        var suggestion = RoadPathfinder.Suggest(map, orders, expert);
        check(route.TryReplace(suggestion.EdgeIds) && route.BuildCompletion(orders, expert) != null, "Suggestion forms a valid editable route");
        route.Undo();
        check(route.BuildCompletion(orders, expert) == null, "Suggestion can be edited");

        // A different graph proves that path search does not depend on Goal1/Goal2 or the sample topology.
        var graph = new[] {
            new MapEdge("AB", "Depot", "Cross", 5, RoadType.Main, 1),
            new MapEdge("AC", "Depot", "Customer", 20, RoadType.Main, 1),
            new MapEdge("BC", "Cross", "Customer", 2, RoadType.Main, 1),
            new MapEdge("BC-fast", "Cross", "Customer", 1, RoadType.Main, 1),
            new MapEdge("CZ", "Customer", "Exit", 1, RoadType.Main, 1, false)
        };
        var path = RoadPathfinder.FindShortestPath(graph, "Depot", "Customer", e => e.Distance);
        check(path.SequenceEqual(new[] { "AB", "BC-fast" }), "Arbitrary graph and parallel roads");
        check(RoadPathfinder.FindShortestPath(graph, "Exit", "Depot", e => e.Distance) == null, "One-way road respected");
        check(RoadPathfinder.FindShortestPath(graph, "Depot", "Missing", e => e.Distance) == null, "Unreachable destination");
        M4Validation.Run();
        checks += M4Validation.LastCheckCount;
        ValidateRevision(map, check, (actual,expected,name)=>check(Mathf.Abs(actual-expected)<.001f,name));
        return $"PASS: {checks} M3/M4 checks (current sample settings).";
    }
}
