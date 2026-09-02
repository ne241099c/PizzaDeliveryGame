using System;
using UnityEngine;

[Serializable]
public class Order
{
    public string Id;
    public string DestinationName;
    public float OrderedAt;
    public float TimeLimit;

    public Order(string destinationName, float orderedAt, float timeLimit)
    {
        Id = Guid.NewGuid().ToString();
        DestinationName = destinationName;
        OrderedAt = orderedAt;
        TimeLimit = timeLimit;
    }
}
