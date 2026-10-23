using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The couriers on the team and the one selected for the next dispatch. Data only: the hub cards and the
/// dispatch lanes show them.</summary>
public class CourierRoster : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private List<DeliveryPerson> deliveryPeople=new()
    {
        new DeliveryPerson("Aoi",0.6f,0.6f,3,0.8f,0.7f,0.8f),
        new DeliveryPerson("Ren",0.5f,0.3f,2,0.5f,0.9f,0.4f)
    };
    [SerializeField,Range(0,1)] private float initialPotentialBonus=.25f;
    private List<DeliveryPerson> startingRoster;
    public DeliveryPerson SelectedPerson {get;private set;}
    public IReadOnlyList<DeliveryPerson> People=>deliveryPeople;
    public event Action SelectionChanged;

    private static DeliveryPerson Copy(DeliveryPerson p)=>p.Clone();
    private void Awake()
    {
        foreach(var person in deliveryPeople)person.InitializePotentials(initialPotentialBonus);
        startingRoster=deliveryPeople.ConvertAll(Copy);
    }
    public bool AddPerson(DeliveryPerson person)
    {
        if(person==null || deliveryPeople.Exists(p=>p.Name==person.Name))return false;
        deliveryPeople.Add(person);
        return true;
    }
    public bool RemovePerson(DeliveryPerson person)
    {
        if(!deliveryPeople.Remove(person))return false;
        if(SelectedPerson==person)ClearSelection();
        return true;
    }
    public void RestoreRoster(IEnumerable<DeliveryPerson> people)
    {
        deliveryPeople=new List<DeliveryPerson>(people);
        // Expand the former two-box ceiling, preserving deliberately limited recruits and earned capacity.
        foreach(var person in deliveryPeople)
            if(person.Potential(CourierAbility.Capacity)==2)person.SetPotential(CourierAbility.Capacity,DeliveryPerson.MaximumCapacity);
        ResetDailyState();
    }
    public void ResetToInitialRoster(){deliveryPeople=startingRoster.ConvertAll(Copy);ResetDailyState();}
    public void ResetDailyState()
    {
        foreach(var p in deliveryPeople){p.Status=DeliveryStatus.Idle;p.CurrentTrip=null;p.LastReturnedAt=0;}
        ClearSelection();
    }
    public bool SelectForDispatch(DeliveryPerson person)
    {
        if(!deliveryPeople.Contains(person) || !person.IsAvailable || clock.Phase!=DayPhase.Open)return false;
        SelectedPerson=person;
        SelectionChanged?.Invoke();
        return true;
    }
    public void ClearSelection()
    {
        SelectedPerson=null;
        SelectionChanged?.Invoke();
    }
}
