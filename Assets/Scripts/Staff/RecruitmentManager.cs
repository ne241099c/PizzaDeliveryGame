using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>A kitchen staff candidate (内勤). Separate from courier applicants so courier generation is unchanged.</summary>
public sealed class KitchenApplicant
{
    public KitchenStaff Staff { get; }
    public KitchenApplicant(KitchenStaff staff) => Staff=staff;
}

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
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private DeliveryDynamics dynamics;
    [SerializeField] private TrainingManager training;
    [SerializeField] private GameClock clock;
    [SerializeField] private CourierRoster roster;
    [SerializeField] private KitchenCrew crew;
    [SerializeField,Range(0,1)] private float kitchenApplicantChance=.6f;
    [SerializeField] private Vector2 kitchenAbilityRange=new(.3f,.8f);
    [SerializeField] private string[] kitchenNamePool={"Sakura","Tsumugi","Kanata","Hiro","Asahi","Ema","Shion","Ritsu"};
    [SerializeField] private BusinessEconomy economy;
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
    [FormerlySerializedAs("newcomerCapacityPotentialTwo"),SerializeField,Range(0,1)] private float newcomerCapacityPotentialThree=.7f;
    [SerializeField,Range(1,DeliveryPerson.MaximumCapacity)] private int capacityGrowthLimit=DeliveryPerson.MaximumCapacity;
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
    private System.Random random,kitchenRandom;
    private readonly List<Applicant> applicants=new();
    private readonly List<KitchenApplicant> kitchenApplicants=new();
    public IReadOnlyList<KitchenApplicant> KitchenApplicants => kitchenApplicants;
    /// <summary>Total team size; hiring has no headcount limit.</summary>
    public int TeamCount => roster.People.Count+(crew!=null?crew.Count:0);
    public int? SeedOverride { get; set; }
    public IReadOnlyList<Applicant> Applicants => applicants;
    public int MaximumPeople => int.MaxValue; // Compatibility for existing tools; no gameplay headcount limit.
    public string LastMessage { get; private set; }="";
    public event Action Changed;
    private void Awake() => ResetRandom();
    public void ResetRandom()
    {
        int seed=SeedOverride ?? (useFixedSeed ? randomSeed : Environment.TickCount);
        random=new System.Random(seed);
        kitchenRandom=new System.Random(unchecked(seed*31+7));
    }
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
        person.SetPotential(CourierAbility.Capacity,type==RecruitArchetype.Newcomer?(random.NextDouble()<newcomerCapacityPotentialThree?capacityGrowthLimit:smallCapacity):capacityGrowthLimit);
        return new Applicant(person,baseline,strong,weak);
    }
    public void RestoreApplicants(IEnumerable<Applicant> restored)
    {applicants.Clear();applicants.AddRange(restored);LastMessage="Applicants restored.";Changed?.Invoke();}
    public void RestoreKitchenApplicants(IEnumerable<KitchenApplicant> restored)
    {
        kitchenApplicants.Clear();
        kitchenApplicants.AddRange(restored);
        Changed?.Invoke();
    }
    public void GenerateDay()
    {
        if(random==null)ResetRandom();applicants.Clear();
        var used=new HashSet<string>(roster.People.Select(p=>p.Name));if(crew!=null)used.UnionWith(crew.People.Select(c=>c.Name));
        var names=AvailableNames(namePool,used,maximumApplicants,"Courier");Shuffle(names);
        int count=Mathf.Min(names.Length,random.Next(minimumApplicants,Mathf.Max(minimumApplicants,maximumApplicants)+1));
        foreach(string name in names.Take(count))applicants.Add(GenerateApplicant(name));
        GenerateKitchenDay();
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
        if(economy.Cash<HiringCost(applicant.Person))return $"Not enough cash. Need {HiringCost(applicant.Person):F2}.";
        return "";
    }
    public bool Hire(Applicant applicant)
    {
        string reason=HireRejection(applicant);if(reason.Length>0)return Reject(reason);
        dynamics?.ResetPerson(applicant.Person);
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

    // ---- kitchen staff (内勤) ----
    private void GenerateKitchenDay()
    {
        if(kitchenRandom==null)ResetRandom();kitchenApplicants.Clear();
        if(kitchenRandom.NextDouble()>=kitchenApplicantChance)return;
        var used=new HashSet<string>(roster.People.Select(p=>p.Name).Concat(applicants.Select(a=>a.Person.Name)));
        if(crew!=null)used.UnionWith(crew.People.Select(c=>c.Name));
        var names=AvailableNames(kitchenNamePool,used,1,"Kitchen");
        float Ability()=>Mathf.Lerp(kitchenAbilityRange.x,kitchenAbilityRange.y,(float)kitchenRandom.NextDouble());
        kitchenApplicants.Add(new KitchenApplicant(new KitchenStaff(names[kitchenRandom.Next(names.Length)],Ability(),Ability())));
    }
    private static string[] AvailableNames(IEnumerable<string> pool,HashSet<string> used,int needed,string fallback)
    {
        var bases=pool.Where(n=>!string.IsNullOrWhiteSpace(n)).Distinct().ToArray();if(bases.Length==0)bases=new[]{fallback};
        var names=bases.Where(n=>!used.Contains(n)).ToList();
        for(int suffix=2;names.Count<needed;suffix++)
            foreach(var name in bases)
            {
                string candidate=name+" "+suffix;
                if(!used.Contains(candidate))names.Add(candidate);
                if(names.Count>=needed)break;
            }
        return names.ToArray();
    }
    public decimal HiringCost(KitchenStaff staff) => BusinessEconomy.Money(economy.DailyWage(staff)*(decimal)hiringWageDays);
    public decimal SeveranceCost(KitchenStaff staff) => BusinessEconomy.Money(economy.DailyWage(staff)*(decimal)severanceWageDays);
    public string HireRejection(KitchenApplicant applicant)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Hiring is available only during preparation.";
        if(crew==null || applicant==null || !kitchenApplicants.Contains(applicant))return "This applicant is no longer available.";
        if(economy.Cash<HiringCost(applicant.Staff))return $"Not enough cash. Need {HiringCost(applicant.Staff):F2}.";
        return "";
    }
    public bool Hire(KitchenApplicant applicant)
    {
        string reason=HireRejection(applicant);if(reason.Length>0)return Reject(reason);
        if(!crew.Add(applicant.Staff))return Reject("A staff member with this name already exists.");
        economy.SpendPreparation(HiringCost(applicant.Staff));kitchenApplicants.Remove(applicant);
        LastMessage=$"Hired {applicant.Staff.Name}. Available today.";Changed?.Invoke();return true;
    }
    public bool Fire(KitchenStaff staff)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return Reject("Firing is available only during preparation.");
        if(crew==null || !crew.People.Contains(staff))return Reject("Staff member is not in the team.");
        economy.SpendPreparation(SeveranceCost(staff));crew.Remove(staff);
        LastMessage=$"Dismissed {staff.Name}; severance paid.";Changed?.Invoke();return true;
    }
    private bool Reject(string reason) { LastMessage=reason;Changed?.Invoke();return false; }
}
