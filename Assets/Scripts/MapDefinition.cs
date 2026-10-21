using System.Collections.Generic;
using UnityEngine;

/// <summary>Road graph data, independent of route UI and destination names.</summary>
[CreateAssetMenu(menuName = "Pizza Delivery/Road Map")]
public class MapDefinition : ScriptableObject
{
    public List<MapNode> Nodes = new();
    public List<MapEdge> Edges = new();
}
