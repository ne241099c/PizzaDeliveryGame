using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class TrainingAssignment
{
    public string PersonId;
    public CourierAbility Ability;
    public string Fee;
    public bool Completed;
    public decimal Paid=>decimal.Parse(Fee,CultureInfo.InvariantCulture);
}
public sealed class TrainingManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private CourierProgression progression;
    [SerializeField,Min(0)] private float baseCost=80;
    [SerializeField,Min(0)] private float abilityCostFactor=150;
    [SerializeField,Min(0)] private float capacityCost=300;
    [SerializeField,Range(0,1)] private float trainingDurationRatio=.5f;
    [SerializeField,Range(0,1)] private float abilityGain=.1f;
    [SerializeField,Min(1)] private int capacityGain=1;
    private readonly List<TrainingAssignment> assignments=new();
    public IReadOnlyList<TrainingAssignment> Assignments=>assignments;
    public string LastMessage {get;private set;}="";
    public event Action Changed;
    public float EndsAt=>clock.BusinessDuration*trainingDurationRatio;
    private void OnEnable()=>clock.Advanced+=Tick;
    private void OnDisable()=>clock.Advanced-=Tick;
    public bool HasAssignment(DeliveryPerson p)=>assignments.Any(a=>a.PersonId==p.Id);
    public decimal Cost(DeliveryPerson p,CourierAbility ability)=>BusinessEconomy.Money((decimal)(ability==CourierAbility.Capacity?capacityCost:baseCost+abilityCostFactor*p.Ability(ability)));
    public string Rejection(DeliveryPerson p,CourierAbility ability)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Training can be ordered only during preparation.";
        if(p==null || !roster.People.Contains(p))return "Choose a courier from your team.";
        if(HasAssignment(p))return "Only one training per courier per day.";
        if(!p.IsAvailable)return "Courier is not available.";
        if(p.Ability(ability)>=p.Potential(ability))return "This ability has reached its potential.";
        if(economy.Cash<Cost(p,ability))return $"Not enough cash for training: {Cost(p,ability):F2}.";
        return "";
    }
    public bool Request(DeliveryPerson p,CourierAbility ability)
    {
        string reason=Rejection(p,ability);if(reason.Length>0)return Reject(reason);
        progression.TrackRoster();decimal fee=Cost(p,ability);economy.SpendPreparation(fee);
        assignments.Add(new TrainingAssignment{PersonId=p.Id,Ability=ability,Fee=fee.ToString(CultureInfo.InvariantCulture)});
        p.Status=DeliveryStatus.Training;LastMessage=$"Training: {p.Name} / {ability}; completes at {EndsAt:F0}s.";Changed?.Invoke();return true;
    }
    public bool Cancel(DeliveryPerson p)
    {
        var assignment=assignments.FirstOrDefault(a=>a.PersonId==p.Id);
        if(clock.Phase!=DayPhase.BeforeOpening || assignment==null || assignment.Completed)return Reject("Cancel training before business starts.");
        economy.RefundPreparation(assignment.Paid);assignments.Remove(assignment);p.Status=DeliveryStatus.Idle;
        LastMessage="Training cancelled; full refund.";Changed?.Invoke();return true;
    }
    private bool Reject(string reason){LastMessage=reason;Changed?.Invoke();return false;}
    public void RestoreAssignments(IEnumerable<TrainingAssignment> restored)
    {
        assignments.Clear();assignments.AddRange(restored);
        foreach(var assignment in assignments.Where(a=>!a.Completed))roster.People.First(p=>p.Id==assignment.PersonId).Status=DeliveryStatus.Training;
        Changed?.Invoke();
    }
    public void PrepareDay(){assignments.Clear();LastMessage="";Changed?.Invoke();}
    private void Tick(float before,float now)
    {
        if(now<EndsAt)return;
        bool changed=false;
        foreach(var assignment in assignments.Where(a=>!a.Completed))
        {
            var person=roster.People.FirstOrDefault(p=>p.Id==assignment.PersonId);if(person==null)continue;
            progression.ApplyTraining(person,assignment.Ability,assignment.Ability==CourierAbility.Capacity?capacityGain:abilityGain);
            assignment.Completed=true;changed=true;person.Status=DeliveryStatus.Idle;LastMessage=$"Training completed: {person.Name} / {assignment.Ability}.";
        }
        if(changed)Changed?.Invoke();
    }
    public string Caption(DeliveryPerson p)
    {
        var a=assignments.FirstOrDefault(a=>a.PersonId==p.Id && !a.Completed);
        return a==null?"Training":$"Training: {a.Ability} ({Mathf.Max(0,EndsAt-clock.ElapsedSeconds):F0}s left)";
    }
}
