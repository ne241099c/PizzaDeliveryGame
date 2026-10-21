using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum DeliveryStatus { Idle, Delivering, Returning }

[Serializable]
public class DeliveryPerson
{
    public string Name;
    [SerializeField, Range(0, 1)] public float Driving = 0.5f;
    [FormerlySerializedAs("SkillLevel"), SerializeField, Range(0, 1)] public float LocalKnowledge;
    [SerializeField, Range(1, 2)] public int Capacity = 2;
    [SerializeField, Range(0, 1)] public float Service = 0.5f;
    [SerializeField, Range(0, 1)] public float Endurance = 0.5f;
    [SerializeField, Range(0, 1)] public float Safety = 0.5f;
    // Compatibility for older route fixtures; runtime calculations use the six abilities.
    public float SkillLevel { get => LocalKnowledge; set => LocalKnowledge = value; }
    public DeliveryStatus Status { get; internal set; }
    public DeliveryTrip CurrentTrip { get; internal set; }
    public bool IsAvailable
    {
        get => Status == DeliveryStatus.Idle;
        set => Status = value ? DeliveryStatus.Idle : DeliveryStatus.Delivering;
    }

    public DeliveryPerson(string name, float driving, float knowledge, int capacity, float service, float endurance, float safety)
        : this(name, knowledge)
    {
        Driving = driving; Capacity = capacity; Service = service; Endurance = endurance; Safety = safety;
    }

    public DeliveryPerson(string name, float skillLevel)
    {
        Name = name;
        LocalKnowledge = skillLevel;
        Status = DeliveryStatus.Idle;
    }
}
