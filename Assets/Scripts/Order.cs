using System;

public enum OrderStatus { Pending, Dispatched, Delivered, Cancelled }

[Serializable]
public class Order
{
    public string Id;
    public string DestinationNodeId;
    public string DestinationName;
    public float SpawnedAt;
    public float Deadline;
    public OrderStatus Status { get; internal set; }
    // Compatibility for M3 callers; all timestamps now use GameClock's seconds.
    public float OrderedAt => SpawnedAt;
    public float TimeLimit => Deadline - SpawnedAt;

    public Order(string destinationNodeId, string destinationName, float orderedAt, float timeLimit)
    {
        Id = Guid.NewGuid().ToString();
        DestinationNodeId = destinationNodeId;
        DestinationName = destinationName;
        SpawnedAt = orderedAt;
        Deadline = orderedAt + timeLimit;
        Status = OrderStatus.Pending;
    }
}
