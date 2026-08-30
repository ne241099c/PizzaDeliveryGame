using System;
using UnityEngine;

[Serializable]
public class Order
{
    public string Id;
    public string DestinationName;
    public float OrderdAt;
    public float TimeLimit;

    public Order(string destinationName, float orderdAt, float timeLimit)
    {
        Id = Guid.NewGuid().ToString();
        DestinationName = destinationName;
        OrderdAt = orderdAt;
        TimeLimit = timeLimit;
    }
}