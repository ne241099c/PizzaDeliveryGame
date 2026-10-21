using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum DeliveryStatus { Idle, Delivering, Returning, Training }
public enum RecruitArchetype { Newcomer, Experienced, Standard }
public enum CourierAbility { Driving, LocalKnowledge, Service, Endurance, Safety, Capacity }

[Serializable]
public class DeliveryPerson : ISerializationCallbackReceiver
{
    public string Name;
    public RecruitArchetype Archetype=RecruitArchetype.Standard;
    [SerializeField] private string id;
    [FormerlySerializedAs("Driving"),SerializeField,Range(0,1)] private float driving=.5f;
    [FormerlySerializedAs("SkillLevel"),FormerlySerializedAs("LocalKnowledge"),SerializeField,Range(0,1)] private float localKnowledge;
    [FormerlySerializedAs("Capacity"),SerializeField,Range(1,2)] private int capacity=2;
    [FormerlySerializedAs("Service"),SerializeField,Range(0,1)] private float service=.5f;
    [FormerlySerializedAs("Endurance"),SerializeField,Range(0,1)] private float endurance=.5f;
    [FormerlySerializedAs("Safety"),SerializeField,Range(0,1)] private float safety=.5f;
    [SerializeField] private float[] potentials={1,1,1,1,1};
    [SerializeField] private float[] experience=new float[5];
    [SerializeField,Range(1,2)] private int capacityPotential=2;
    [SerializeField] private DeliveryStatus status;
    public string Id => string.IsNullOrEmpty(id) ? id=Guid.NewGuid().ToString("N") : id;
    public float Driving {get=>driving;set=>driving=Mathf.Clamp(value,0,Potential(CourierAbility.Driving));}
    public float LocalKnowledge {get=>localKnowledge;set=>localKnowledge=Mathf.Clamp(value,0,Potential(CourierAbility.LocalKnowledge));}
    public float Service {get=>service;set=>service=Mathf.Clamp(value,0,Potential(CourierAbility.Service));}
    public float Endurance {get=>endurance;set=>endurance=Mathf.Clamp(value,0,Potential(CourierAbility.Endurance));}
    public float Safety {get=>safety;set=>safety=Mathf.Clamp(value,0,Potential(CourierAbility.Safety));}
    public int Capacity {get=>capacity;set=>capacity=Mathf.Clamp(value,1,capacityPotential);}
    public float SkillLevel {get=>LocalKnowledge;set=>LocalKnowledge=value;}
    public float CurrentStamina {get;internal set;}=1f;
    public float LastReturnedAt {get;internal set;}
    public DeliveryStatus Status {get=>status;internal set=>status=value;}
    public DeliveryTrip CurrentTrip {get;internal set;}
    public bool IsAvailable {get=>Status==DeliveryStatus.Idle;set=>Status=value?DeliveryStatus.Idle:DeliveryStatus.Delivering;}
    public bool CanCarry(int count)=>count>0 && count<=Capacity;
    public float Ability(CourierAbility ability) => ability switch
    { CourierAbility.Driving=>Driving,CourierAbility.LocalKnowledge=>LocalKnowledge,CourierAbility.Service=>Service,CourierAbility.Endurance=>Endurance,CourierAbility.Safety=>Safety,_=>Capacity };
    public void SetAbility(CourierAbility ability,float value)
    {
        switch(ability)
        {case CourierAbility.Driving:Driving=value;break;case CourierAbility.LocalKnowledge:LocalKnowledge=value;break;case CourierAbility.Service:Service=value;break;case CourierAbility.Endurance:Endurance=value;break;case CourierAbility.Safety:Safety=value;break;case CourierAbility.Capacity:Capacity=Mathf.FloorToInt(value);break;}
    }
    public float Potential(CourierAbility ability) {EnsureData();return ability==CourierAbility.Capacity?capacityPotential:potentials[(int)ability];}
    public float Experience(CourierAbility ability) {EnsureData();return ability==CourierAbility.Capacity?0:experience[(int)ability];}
    public void SetPotential(CourierAbility ability,float value)
    {
        EnsureData();if(ability==CourierAbility.Capacity)capacityPotential=Mathf.Clamp(Mathf.FloorToInt(value),1,2);else potentials[(int)ability]=Mathf.Clamp01(value);
        SetAbility(ability,Ability(ability));if(ability!=CourierAbility.Capacity && Ability(ability)>=Potential(ability))experience[(int)ability]=0;
    }
    public void SetExperience(CourierAbility ability,float value)
    {EnsureData();if(ability!=CourierAbility.Capacity)experience[(int)ability]=Ability(ability)>=Potential(ability)?0:Mathf.Max(0,value);}
    public void InitializePotentials(float bonus)
    {for(int i=0;i<5;i++)SetPotential((CourierAbility)i,Ability((CourierAbility)i)+bonus);SetPotential(CourierAbility.Capacity,2);}
    public DeliveryPerson Clone() { _=Id;return JsonUtility.FromJson<DeliveryPerson>(JsonUtility.ToJson(this)); }
    private void EnsureData()
    {if(potentials==null || potentials.Length!=5)potentials=new[]{1f,1f,1f,1f,1f};if(experience==null || experience.Length!=5)experience=new float[5];}
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {EnsureData();capacityPotential=Mathf.Clamp(capacityPotential,1,2);for(int i=0;i<6;i++)SetPotential((CourierAbility)i,Potential((CourierAbility)i));}
    public DeliveryPerson(string name,float driving,float knowledge,int capacity,float service,float endurance,float safety):this(name,knowledge)
    {Driving=driving;Capacity=capacity;Service=service;Endurance=endurance;Safety=safety;}
    public DeliveryPerson(string name,float skillLevel) {Name=name;LocalKnowledge=skillLevel;Status=DeliveryStatus.Idle;}
}
