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
public enum KitchenSkill { Speed, Care }
/// <summary>A paid kitchen upgrade. Completed histories never apply a gain twice.</summary>
[Serializable]
public sealed class KitchenTrainingAssignment
{
    public string StaffId;
    public KitchenSkill Skill;
    public string Fee;
    public bool Completed;
    public decimal Paid=>decimal.Parse(Fee,CultureInfo.InvariantCulture);
}
public sealed class TrainingManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private CourierRoster roster;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private CourierProgression progression;
    [SerializeField] private KitchenCrew crew;
    [SerializeField,Min(0)] private float baseCost=80;
    [SerializeField,Min(0)] private float abilityCostFactor=150;
    [SerializeField,Min(0)] private float capacityCost=300;
    [SerializeField,Range(0,1)] private float abilityGain=.1f;
    [SerializeField,Min(1)] private int capacityGain=1;
    private readonly List<TrainingAssignment> assignments=new();
    public IReadOnlyList<TrainingAssignment> Assignments=>assignments;
    private readonly List<KitchenTrainingAssignment> kitchenAssignments=new();
    public IReadOnlyList<KitchenTrainingAssignment> KitchenAssignments=>kitchenAssignments;
    public float AbilityGain=>abilityGain;
    private KitchenCrew Crew
    {
        get{if(crew==null)crew=FindFirstObjectByType<KitchenCrew>();return crew;}
    }
    public string LastMessage {get;private set;}="";
    public event Action Changed;
    // Retained for callers of the previous timed-training API: purchases finish immediately.
    public float EndsAt=>clock.ElapsedSeconds;
    public int CapacityGain=>capacityGain;
    public bool HasAssignment(DeliveryPerson p)=>assignments.Any(a=>a.PersonId==p.Id && !a.Completed);
    public decimal Cost(DeliveryPerson p,CourierAbility ability)=>BusinessEconomy.Money((decimal)(ability==CourierAbility.Capacity?capacityCost:baseCost+abilityCostFactor*p.Ability(ability)));
    public string Rejection(DeliveryPerson p,CourierAbility ability)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Training can be ordered only during preparation.";
        if(p==null || !roster.People.Contains(p))return "Choose a courier from your team.";
        if(!p.IsAvailable)return "Courier is not available.";
        if(p.Ability(ability)>=p.Potential(ability))return "This ability has reached its potential.";
        if(economy.Cash<Cost(p,ability))return $"Not enough cash for training: {Cost(p,ability):F2}.";
        return "";
    }
    public bool Request(DeliveryPerson p,CourierAbility ability)
    {
        string reason=Rejection(p,ability);if(reason.Length>0)return Reject(reason);
        progression.TrackRoster();decimal fee=Cost(p,ability);economy.SpendPreparation(fee);
        var assignment=new TrainingAssignment{PersonId=p.Id,Ability=ability,Fee=fee.ToString(CultureInfo.InvariantCulture)};
        assignments.Add(assignment);Complete(assignment);
        LastMessage=ShopStrings.InstantTrainingResult(p.Name,ShopStrings.Ability(ability));Changed?.Invoke();return true;
    }
    public bool Cancel(DeliveryPerson p)=>Reject(ShopStrings.InstantTrainingNoRefund);
    private bool Reject(string reason){LastMessage=reason;Changed?.Invoke();return false;}

    // ---- kitchen staff ----
    public static float Skill(KitchenStaff staff,KitchenSkill skill)=>skill==KitchenSkill.Speed?staff.Speed:staff.Care;
    public bool HasAssignment(KitchenStaff staff)=>kitchenAssignments.Any(a=>a.StaffId==staff.Id && !a.Completed);
    public KitchenTrainingAssignment AssignmentOf(KitchenStaff staff)=>kitchenAssignments.FirstOrDefault(a=>a.StaffId==staff.Id && !a.Completed);
    public decimal Cost(KitchenStaff staff,KitchenSkill skill)=>BusinessEconomy.Money((decimal)(baseCost+abilityCostFactor*Skill(staff,skill)));
    public string Rejection(KitchenStaff staff,KitchenSkill skill)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "訓練は開店準備のあいだだけ申し込めます。";
        if(staff==null || Crew==null || !Crew.People.Contains(staff))return "厨房のスタッフを選んでください。";
        if(Skill(staff,skill)>=1)return "この能力はもう最高です。";
        if(economy.Cash<Cost(staff,skill))return $"訓練の費用が足りません（{UiKit.Yen(Cost(staff,skill))}）。";
        return "";
    }
    public bool Request(KitchenStaff staff,KitchenSkill skill)
    {
        string reason=Rejection(staff,skill);if(reason.Length>0)return Reject(reason);
        decimal fee=Cost(staff,skill);economy.SpendPreparation(fee);
        var assignment=new KitchenTrainingAssignment{StaffId=staff.Id,Skill=skill,Fee=fee.ToString(CultureInfo.InvariantCulture)};
        kitchenAssignments.Add(assignment);Complete(assignment);
        LastMessage=ShopStrings.InstantTrainingResult(staff.Name,ShopStrings.KitchenSkill(skill));Changed?.Invoke();return true;
    }
    public bool Cancel(KitchenStaff staff)=>Reject(ShopStrings.InstantTrainingNoRefund);
    public void RestoreKitchenAssignments(IEnumerable<KitchenTrainingAssignment> restored)
    {
        kitchenAssignments.Clear();if(restored!=null)kitchenAssignments.AddRange(restored);
        foreach(var assignment in kitchenAssignments)Complete(assignment);
        if(Crew!=null)foreach(var staff in Crew.People)staff.InTraining=false;
        Changed?.Invoke();
    }
    public void RestoreAssignments(IEnumerable<TrainingAssignment> restored)
    {
        assignments.Clear();if(restored!=null)assignments.AddRange(restored);
        foreach(var assignment in assignments)Complete(assignment);
        foreach(var person in roster.People)if(person.Status==DeliveryStatus.Training)person.Status=DeliveryStatus.Idle;
        Changed?.Invoke();
    }
    public void PrepareDay()
    {
        assignments.Clear();kitchenAssignments.Clear();LastMessage="";
        foreach(var person in roster.People)if(person.Status==DeliveryStatus.Training)person.Status=DeliveryStatus.Idle;
        if(Crew!=null)foreach(var staff in Crew.People)staff.InTraining=false;
        Changed?.Invoke();
    }
    // Old paid reservations are fulfilled once on load, without charging again.
    private void Complete(TrainingAssignment assignment)
    {
        if(assignment.Completed)return;
        var person=roster.People.FirstOrDefault(p=>p.Id==assignment.PersonId);
        if(person!=null)
        {
            progression.ApplyTraining(person,assignment.Ability,assignment.Ability==CourierAbility.Capacity?capacityGain:abilityGain);
            if(person.Status==DeliveryStatus.Training)person.Status=DeliveryStatus.Idle;
        }
        assignment.Completed=true;
    }
    private void Complete(KitchenTrainingAssignment assignment)
    {
        if(assignment.Completed)return;
        var staff=Crew!=null?Crew.People.FirstOrDefault(s=>s.Id==assignment.StaffId):null;
        if(staff!=null)
        {
            if(assignment.Skill==KitchenSkill.Speed)staff.Speed+=abilityGain;else staff.Care+=abilityGain;
            staff.InTraining=false;
        }
        assignment.Completed=true;
    }
    public string Caption(DeliveryPerson p)=>ShopStrings.Train;
}
