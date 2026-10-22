using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CourierProgression : MonoBehaviour
{
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField,Min(1)] private float experiencePerLevel=200;
    [SerializeField,Range(.0001f,1)] private float growthPerLevel=.02f;
    [SerializeField,Min(0)] private float drivingExperiencePerDistance=.3f;
    [SerializeField,Min(0)] private float knowledgeExperiencePerRoad=15;
    [SerializeField,Min(0)] private float onTimeServiceExperience=15;
    [SerializeField,Min(0)] private float lateServiceExperience=5;
    [SerializeField,Min(0)] private float enduranceExperiencePerSecond=.35f;
    [SerializeField,Min(0)] private float safetyExperiencePerRoad=4;
    [SerializeField] private bool automaticGrowth=true;
    private readonly List<string> growth=new();
    private readonly Dictionary<string,float[]> dailyStart=new();
    private readonly Dictionary<string,float[]> experienceGrowth=new();
    public float ExperiencePerLevel=>experiencePerLevel;
    public float GrowthPerLevel=>growthPerLevel;
    public int GrowthCount=>growth.Count;
    public string DailySummary=>growth.Count==0?"Growth: none":"Growth\n"+string.Join("\n",growth);
    private void OnEnable() {if(flow!=null){flow.Returned+=Return;flow.Dispatched+=TrackNewPerson;}}
    private void OnDisable() {if(flow!=null){flow.Returned-=Return;flow.Dispatched-=TrackNewPerson;}}
    public void PrepareDay()
    {
        growth.Clear();dailyStart.Clear();experienceGrowth.Clear();foreach(var person in roster.People)dailyStart[person.Id]=Values(person);
    }
    private void TrackNewPerson(DeliveryTrip trip) {if(!dailyStart.ContainsKey(trip.Person.Id))dailyStart[trip.Person.Id]=Values(trip.Person);}
    public void TrackRoster() {foreach(var p in roster.People)if(!dailyStart.ContainsKey(p.Id))dailyStart[p.Id]=Values(p);}
    public static float[] Values(DeliveryPerson p)=>Enumerable.Range(0,6).Select(i=>p.Ability((CourierAbility)i)).ToArray();
    public float ExperienceEarned(DeliveryTrip trip,CourierAbility ability)=>ability switch
    {
        CourierAbility.Driving=>trip.DistanceAt(trip.ReturnsAt)*drivingExperiencePerDistance,
        CourierAbility.LocalKnowledge=>trip.BackstreetCount*knowledgeExperiencePerRoad,
        CourierAbility.Service=>trip.Deliveries.Sum(d=>d.ArrivesAt<=d.Order.Deadline?onTimeServiceExperience:lateServiceExperience),
        CourierAbility.Endurance=>(trip.ReturnsAt-trip.StartedAt)*enduranceExperiencePerSecond,
        CourierAbility.Safety=>trip.SafeRoadCount*safetyExperiencePerRoad,_=>0
    };
    private void Return(DeliveryTrip trip)
    {if(automaticGrowth)for(int i=0;i<5;i++)AwardExperience(trip.Person,(CourierAbility)i,ExperienceEarned(trip,(CourierAbility)i));}
    public void AwardExperience(DeliveryPerson person,CourierAbility ability,float amount)
    {
        if(ability==CourierAbility.Capacity || person.Ability(ability)>=person.Potential(ability))return;
        float xp=person.Experience(ability)+Mathf.Max(0,amount),before=person.Ability(ability);
        while(xp>=experiencePerLevel && person.Ability(ability)<person.Potential(ability))
        {xp-=experiencePerLevel;person.SetAbility(ability,person.Ability(ability)+growthPerLevel);}
        person.SetExperience(ability,xp);RecordChange(person,ability,before,"Experience");
    }
    public void ApplyTraining(DeliveryPerson person,CourierAbility ability,float increase)
    {float before=person.Ability(ability);person.SetAbility(ability,before+increase);if(person.Ability(ability)>=person.Potential(ability))person.SetExperience(ability,0);RecordChange(person,ability,before,"Training");}
    private void RecordChange(DeliveryPerson p,CourierAbility a,float before,string source)
    {
        if(p.Ability(a)<=before)return;growth.Add($"{p.Name} / {a}: {before:F2} -> {p.Ability(a):F2} ({source})");
        if(source=="Experience"){if(!experienceGrowth.ContainsKey(p.Id))experienceGrowth[p.Id]=new float[6];experienceGrowth[p.Id][(int)a]+=p.Ability(a)-before;}
    }
    public string GrowthCsv()=>string.Join(";",roster.People.Select(p=>p.Name+":"+string.Join("/",Values(p).Select((v,i)=>(v-(dailyStart.TryGetValue(p.Id,out var start)?start[i]:v)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)))));
    public string ExperienceGrowthCsv()=>string.Join(";",roster.People.Select(p=>p.Name+":"+string.Join("/",Enumerable.Range(0,6).Select(i=>(experienceGrowth.TryGetValue(p.Id,out var gains)?gains[i]:0).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)))));
    public string PotentialsCsv()=>string.Join(";",roster.People.Select(p=>p.Name+":"+string.Join("/",Enumerable.Range(0,6).Select(i=>p.Potential((CourierAbility)i).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)))));
    public string AbilitiesCsv()=>string.Join(";",roster.People.Select(p=>p.Name+":"+string.Join("/",Values(p).Select(v=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)))));
}
