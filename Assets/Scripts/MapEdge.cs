using System;

[Serializable]
public class MapEdge
{
    public string Id;
    public string NodeAId;
    public string NodeBId;
    public float Distance;
    public RoadType RoadType;
    public float TrafficFactor;

    public MapEdge(
        string id,
        string nodeAId,
        string nodeBId,
        float distance,
        RoadType roadType,
        float trafficFactor)
    {
        Id = id;
        NodeAId = nodeAId;
        NodeBId = nodeBId;
        Distance = distance;
        RoadType = roadType;
        TrafficFactor = trafficFactor;
    }
}