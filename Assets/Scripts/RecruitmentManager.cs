using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public sealed class Applicant
{
    public DeliveryPerson Person { get; }
    public RecruitArchetype Type=>Person.Archetype;
    public IReadOnlyList<float> Baselines { get; }
    public IReadOnlyList<int> Strengths { get; }
    public IReadOnlyList<int> Weaknesses { get; }
    public Applicant(DeliveryPerson person,float[] baselines,int[] strengths,int[] weaknesses)
    { Person=person;Baselines=Array.AsReadOnly(baselines);Strengths=Array.AsReadOnly(strengths);Weaknesses=Array.AsReadOnly(weaknesses); }
}

/// <summary>Preparation-only hiring, with reproducible constrained applicant generation.</summary>
public sealed class RecruitmentManager : MonoBehaviour
{
    [SerializeField] private TrainingManager training;
    [SerializeField] private GameClock clock;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField, Min(1)] private int maximumPeople = 4;
    [SerializeField, Min(1)] private int minimumApplicants = 2;
    [SerializeField, Min(1)] private int maximumApplicants = 3;
    [SerializeField, Min(0)] private float hiringWageDays = 3f;
    [SerializeField, Min(0)] private float severanceWageDays = 1f;
    [SerializeField,Range(0,1)] private float newcomerWeight=.3f;
    [SerializeField,Range(0,1)] private float experiencedWeight=.3f;
    [SerializeField,Range(0,1)] private float standardWeight=.4f;
    [SerializeField] private Vector2 newcomerBaseline=new(.2f,.5f);
    [SerializeField] private Vector2 experiencedBaseline=new(.5f,.8f);
    [SerializeField] private Vector2 newcomerPotential=new(.3f,.5f);
    [SerializeField] private Vector2 experiencedPotential=new(.05f,.15f);
    [SerializeField] private Vector2 standardPotential=new(.15f,.3f);
    [SerializeField,Range(0,1)] private float newcomerCapacityPotentialTwo=.7f;
    [SerializeField,Range(0,1)] private float experiencedCapacityTwo=.6f;
    [SerializeField,Min(0)] private float potentialHiringFactor=400;
    [SerializeField] private Vector2 baselineRange = new(.3f,.7f);
    [SerializeField] private Vector2 advantageRange = new(.2f,.3f);
    [SerializeField, Range(0,2)] private int strengthCount = 2;
    [SerializeField, Range(0,2)] private int weaknessCount = 2;
    [SerializeField, Range(0,1)] private float twoOrderProbability = .5f;
    [SerializeField, Min(1)] private int smallCapacity = 1;
    [SerializeField, Min(1)] private int largeCapacity = 2;
    [SerializeField] private string[] namePool = {"Minato","Sora","Haru","Yui","Kai","Riku","Mei","Rio","Nagi","Hina","Itsuki","Towa","Akari","Kohaku"};
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int randomSeed = 4351;
    private System.Random random;
    private readonly List<Applicant> applicants=new();
    public int? SeedOverride { get; set; }
    public IReadOnlyList<Applicant> Applicants => applicants;
    public int MaximumPeople => maximumPeople;
    public string LastMessage { get; private set; }="";
    public event Action Changed;
    private void Awake() => ResetRandom();
    public void ResetRandom() => random=new System.Random(SeedOverride ?? (useFixedSeed ? randomSeed : Environment.TickCount));
    private float Range(Vector2 range) => Mathf.Lerp(range.x,range.y,(float)random.NextDouble());
    private void Shuffle<T>(T[] values)
    {
        for(int i=values.Length-1;i>0;i--) { int j=random.Next(i+1);(values[i],values[j])=(values[j],values[i]); }
    }
    public RecruitArchetype TypeForDraw(double draw)
    {
        double total=newcomerWeight+experiencedWeight+standardWeight;
        return draw*total<newcomerWeight?RecruitArchetype.Newcomer:draw*total<newcomerWeight+experiencedWeight?RecruitArchetype.Experienced:RecruitArchetype.Standard;
    }
    public Applicant GenerateApplicant(string name)
    {
        if(random==null)ResetRandom();
        var type=TypeForDraw(random.NextDouble());
        Vector2 basis=type==RecruitArchetype.Newcomer?newcomerBaseline:type==RecruitArchetype.Experienced?experiencedBaseline:baselineRange;
        Vector2 potential=type==RecruitArchetype.Newcomer?newcomerPotential:type==RecruitArchetype.Experienced?experiencedPotential:standardPotential;
        var baseline=Enumerable.Range(0,5).Select(_=>Range(basis)).ToArray();
        var skills=(float[])baseline.Clone();var indices=Enumerable.Range(0,5).ToArray();Shuffle(indices);
        var strong=indices.Take(strengthCount).ToArray();var weak=indices.Skip(strengthCount).Take(weaknessCount).ToArray();
        foreach(int i in strong)skills[i]=Mathf.Clamp01(skills[i]+Range(advantageRange));
        foreach(int i in weak)skills[i]=Mathf.Clamp01(skills[i]-Range(advantageRange));
        int capacity=type==RecruitArchetype.Newcomer?smallCapacity:random.NextDouble()<(type==RecruitArchetype.Experienced?experiencedCapacityTwo:twoOrderProbability)?largeCapacity:smallCapacity;
        var person=new DeliveryPerson(name,skills[0],skills[1],capacity,skills[2],skills[3],skills[4]){Archetype=type};
        for(int i=0;i<5;i++)person.SetPotential((CourierAbility)i,skills[i]+Range(potential));
        person.SetPotential(CourierAbility.Capacity,type==RecruitArchetype.Newcomer?(random.NextDouble()<newcomerCapacityPotentialTwo?largeCapacity:smallCapacity):largeCapacity);
        return new Applicant(person,baseline,strong,weak);
    }
    public void RestoreApplicants(IEnumerable<Applicant> restored)
    {applicants.Clear();applicants.AddRange(restored);LastMessage="Applicants restored.";Changed?.Invoke();}
    public void GenerateDay()
    {
        if(random==null)ResetRandom();applicants.Clear();
        var names=namePool.Where(n=>!string.IsNullOrWhiteSpace(n)).Distinct().Except(roster.People.Select(p=>p.Name)).ToArray();Shuffle(names);
        int count=Mathf.Min(names.Length,random.Next(minimumApplicants,Mathf.Max(minimumApplicants,maximumApplicants)+1));
        foreach(string name in names.Take(count))applicants.Add(GenerateApplicant(name));
        LastMessage=count<minimumApplicants ? "Not enough unique names in the applicant name pool." : "Choose your team, then start business.";
        Changed?.Invoke();
    }
    public decimal HiringCost(DeliveryPerson person) => BusinessEconomy.Money(economy.DailyWage(person)*(decimal)hiringWageDays
        +(decimal)potentialHiringFactor*Enumerable.Range(0,5).Average(i=>(decimal)person.Potential((CourierAbility)i)-(decimal)person.Ability((CourierAbility)i)));
    public decimal SeveranceCost(DeliveryPerson person) => BusinessEconomy.Money(economy.DailyWage(person)*(decimal)severanceWageDays);
    public string HireRejection(Applicant applicant)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Hiring is available only during preparation.";
        if(applicant==null || !applicants.Contains(applicant))return "This applicant is no longer available.";
        if(roster.People.Count>=maximumPeople)return $"Team limit: {maximumPeople}.";
        if(economy.Cash<HiringCost(applicant.Person))return $"Not enough cash. Need {HiringCost(applicant.Person):F2}.";
        return "";
    }
    public bool Hire(Applicant applicant)
    {
        string reason=HireRejection(applicant);if(reason.Length>0)return Reject(reason);
        if(!roster.AddPerson(applicant.Person))return Reject("A courier with this name already exists.");
        economy.SpendPreparation(HiringCost(applicant.Person));applicants.Remove(applicant);
        LastMessage=$"Hired {applicant.Person.Name}. Available today.";Changed?.Invoke();return true;
    }
    public bool Fire(DeliveryPerson person)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return Reject("Firing is available only during preparation.");
        if(!roster.People.Contains(person))return Reject("Courier is not in the team.");
        if(roster.People.Count<=1)return Reject("Keep at least one courier.");
        if(training!=null && training.HasAssignment(person))return Reject("Cancel training before firing this courier.");
        if(!person.IsAvailable)return Reject("This courier is travelling.");
        economy.SpendPreparation(SeveranceCost(person));roster.RemovePerson(person);
        LastMessage=$"Dismissed {person.Name}; severance paid.";Changed?.Invoke();return true;
    }
    private bool Reject(string reason) { LastMessage=reason;Changed?.Invoke();return false; }
}
