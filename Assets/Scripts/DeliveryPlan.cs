using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A complete trip plan, including the return to the store.</summary>
public sealed class DeliveryPlan
{
    public string Title { get; }
    public string RouteDescription { get; }
    public float TotalTime { get; }
    public bool IsBatch { get; }
    public IReadOnlyList<Order> Orders { get; }
    public IReadOnlyList<string> EdgeIds { get; }

    public DeliveryPlan(string title, string route, float totalTime, bool isBatch, IEnumerable<Order> orders, IEnumerable<string> edgeIds = null)
    {
        Title = title;
        RouteDescription = route;
        TotalTime = totalTime;
        IsBatch = isBatch;
        Orders = Array.AsReadOnly(orders.ToArray());
        EdgeIds = Array.AsReadOnly((edgeIds ?? Array.Empty<string>()).ToArray());
    }
}
