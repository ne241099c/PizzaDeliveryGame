using System;
using UnityEngine;

[Serializable]
public class Order
{
    public string Id;
    public string DestinationNodeId;
    public string DestinationName;
    public float OrderedAt;
    public float TimeLimit;

    public Order(string destinationNodeId, string destinationName, float orderedAt, float timeLimit)
    {
        Id = Guid.NewGuid().ToString();
        DestinationNodeId = destinationNodeId;
        DestinationName = destinationName;
        OrderedAt = orderedAt;
        TimeLimit = timeLimit;
    }
}
