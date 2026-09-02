using System;
using UnityEngine;

[Serializable]
public class MapNode
{
    public string Id;
    public Vector2 Position;
    public NodeType NodeType;
    public bool IsStore;

    public MapNode(string id, Vector2 position, NodeType nodeType, bool isStore)
    {
        Id = id;
        Position = position;
        NodeType = nodeType;
        IsStore = isStore;
    }
}