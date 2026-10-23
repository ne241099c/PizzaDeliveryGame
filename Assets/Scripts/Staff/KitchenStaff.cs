using System;
using UnityEngine;

/// <summary>Kitchen staff (内勤). Speed (手際) shortens non-oven steps; care (丁寧さ) raises quality.</summary>
[Serializable]
public sealed class KitchenStaff
{
    public string Name;
    [SerializeField] private string id;
    [SerializeField,Range(0,1)] private float speed=.4f;
    [SerializeField,Range(0,1)] private float care=.4f;
    public string Id=>string.IsNullOrEmpty(id)?id=Guid.NewGuid().ToString("N"):id;
    public bool HasPersistentId=>!string.IsNullOrWhiteSpace(id);
    public float Speed {get=>speed;set=>speed=Mathf.Clamp01(value);}
    public float Care {get=>care;set=>care=Mathf.Clamp01(value);}
    /// <summary>Set by the kitchen while a staff member is assembling or packing.</summary>
    public bool IsWorking {get;internal set;}
    /// <summary>Away for training during the first half of today's business (not saved: rebuilt from the assignment).</summary>
    public bool InTraining {get;internal set;}
    public KitchenStaff(string name,float speed,float care){Name=name;Speed=speed;Care=care;}
    public KitchenStaff Clone(){_=Id;return JsonUtility.FromJson<KitchenStaff>(JsonUtility.ToJson(this));}
}
