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
    public bool Bidirectional = true;

    public MapEdge(
        string id,
        string nodeAId,
        string nodeBId,
        float distance,
        RoadType roadType,
        float trafficFactor,
        bool bidirectional = true)
    {
        Id = id;
        NodeAId = nodeAId;
        NodeBId = nodeBId;
        Distance = distance;
        RoadType = roadType;
        TrafficFactor = trafficFactor;
        Bidirectional = bidirectional;
    }

    public bool TryTraverse(string fromNodeId, out string toNodeId)
    {
        if (fromNodeId == NodeAId) { toNodeId = NodeBId; return true; }
        if (Bidirectional && fromNodeId == NodeBId) { toNodeId = NodeAId; return true; }
        toNodeId = null;
        return false;
    }
}
