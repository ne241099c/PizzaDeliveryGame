using System;
using UnityEngine;

[Serializable]
public class DeliveryPerson
{
    public string Name;
    public float SkillLevel;
    public bool IsAvailable;

    public DeliveryPerson(string name, float skillLevel)
    {
        Name = name;
        SkillLevel = skillLevel;
        IsAvailable = true;
    }
}