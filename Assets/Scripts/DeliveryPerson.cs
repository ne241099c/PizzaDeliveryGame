using System;
using UnityEngine;

public enum DeliveryStatus { Idle, Delivering, Returning }

[Serializable]
public class DeliveryPerson
{
    public string Name;
    [Range(0, 1)] public float SkillLevel;
    public DeliveryStatus Status { get; internal set; }
    public DeliveryTrip CurrentTrip { get; internal set; }
    public bool IsAvailable
    {
        get => Status == DeliveryStatus.Idle;
        set => Status = value ? DeliveryStatus.Idle : DeliveryStatus.Delivering;
    }

    public DeliveryPerson(string name, float skillLevel)
    {
        Name = name;
        SkillLevel = skillLevel;
        Status = DeliveryStatus.Idle;
    }
}
