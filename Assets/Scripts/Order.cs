using System;

public enum BakeQuality { Standard, Perfect }

public enum BakingState { Waiting, Cooking, Ready }

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
    public BakeQuality Quality {get;internal set;}
    public BakingState Baking {get;internal set;}
    public float CookStartedAt {get;internal set;}=-1;
    public float CookFinishesAt {get;internal set;}
    public bool IsReady => Baking==BakingState.Ready;
    // Compatibility for M3 callers; all timestamps now use GameClock's seconds.
    public float OrderedAt => SpawnedAt;
    public float TimeLimit => Deadline - SpawnedAt;

    public Order(string destinationNodeId, string destinationName, float orderedAt, float timeLimit, BakingState baking=BakingState.Waiting)
    {
        Id = Guid.NewGuid().ToString();
        DestinationNodeId = destinationNodeId;
        DestinationName = destinationName;
        SpawnedAt = orderedAt;
        Deadline = orderedAt + timeLimit;
        Status = OrderStatus.Pending;
        Baking=baking;
    }
}
