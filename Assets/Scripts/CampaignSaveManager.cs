using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class ApplicantSaveData
{
    public DeliveryPerson person;public float[] baselines;public int[] strengths,weaknesses;
}
[Serializable] public sealed class CampaignSaveData
{
    public int version,day,totalDelivered,totalScore,negativeDays;
    public string cash,openingCash,preparationCosts,totalRevenue,bestDailyRevenue;
    public float reputation,promise;
    public List<DeliveryPerson> people;
    public List<ApplicantSaveData> applicants;
    public List<TrainingAssignment> training;
}
/// <summary>Only preparation snapshots are saved. Simulations/tests suppress all normal file changes.</summary>
public sealed class CampaignSaveManager : MonoBehaviour
{
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private GameClock clock;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private RecruitmentManager recruitment;
    [SerializeField] private TrainingManager training;
    [SerializeField] private OrderSpawner spawner;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button continueButton,newButton;
    [SerializeField] private TextMeshProUGUI notice,choiceText;
    [SerializeField] private string filename="campaign.json";
    [SerializeField,Min(1)] private int formatVersion=1;
    private bool ready,restoring,listenersRegistered;
    public bool AutomaticSavingEnabled {get;set;}=true;
    public string SavePathOverride {get;set;}
    public string SavePath=>SavePathOverride??Path.Combine(Application.persistentDataPath,filename);
    public bool AwaitingChoice {get;private set;}
    public string LastMessage {get;private set;}="";
    public void ClearNotice()=>LastMessage="";
    private void OnEnable()
    {campaign.PreparationEntered+=SavePreparation;recruitment.Changed+=SavePreparation;training.Changed+=SavePreparation;spawner.PolicyChanged+=SavePreparation;}
    private void OnDisable()
    {campaign.PreparationEntered-=SavePreparation;recruitment.Changed-=SavePreparation;training.Changed-=SavePreparation;spawner.PolicyChanged-=SavePreparation;}
    private void Update() {if(notice!=null)notice.text=LastMessage;}
    public static decimal Decimal(string value)=>decimal.Parse(value,NumberStyles.Number,CultureInfo.InvariantCulture);
    public void Startup()
    {
        if(!listenersRegistered){continueButton.onClick.AddListener(Continue);newButton.onClick.AddListener(StartNewGame);listenersRegistered=true;}
        if(File.Exists(SavePath) && TryRead(out var data,out var reason))
        {ready=false;AwaitingChoice=true;UIVisibility.Set(choicePanel,true);choiceText.text=$"CONTINUE CAMPAIGN?\nDay {data.day} / Cash {data.cash} / Reputation {data.reputation:F1}";return;}
        if(File.Exists(SavePath))LastMessage="Cannot load save: "+ReadFailure()+" Starting a new game.";
        StartNewGame();
    }
    private string ReadFailure(){TryRead(out _,out var reason);return reason;}
    public void StartNewGame()
    {AwaitingChoice=false;ready=true;UIVisibility.Set(choicePanel,false);campaign.NewGame();}
    public void Continue()
    {
        if(TryRead(out var data,out var reason))
        {restoring=true;try{campaign.RestorePreparation(data);}finally{restoring=false;}ready=true;AwaitingChoice=false;UIVisibility.Set(choicePanel,false);LastMessage=$"Loaded day {data.day}.";}
        else {LastMessage="Cannot load save: "+reason+" Starting a new game.";StartNewGame();}
    }
    public CampaignSaveData Capture()
    {
        if(clock.Phase!=DayPhase.BeforeOpening)throw new InvalidOperationException("Only preparation can be saved.");
        return new CampaignSaveData{version=formatVersion,day=campaign.DayNumber,totalDelivered=campaign.TotalDelivered,totalScore=campaign.TotalScore,negativeDays=campaign.ConsecutiveNegativeDays,
            cash=economy.Cash.ToString(CultureInfo.InvariantCulture),openingCash=economy.OpeningCash.ToString(CultureInfo.InvariantCulture),preparationCosts=economy.Ledger.PreparationCosts.ToString(CultureInfo.InvariantCulture),
            totalRevenue=campaign.TotalRevenue.ToString(CultureInfo.InvariantCulture),bestDailyRevenue=campaign.BestDailyRevenue.ToString(CultureInfo.InvariantCulture),reputation=reputation.Value,promise=spawner.PromiseSeconds,
            people=roster.People.Select(p=>p.Clone()).ToList(),
            applicants=recruitment.Applicants.Select(a=>new ApplicantSaveData{person=a.Person.Clone(),baselines=a.Baselines.ToArray(),strengths=a.Strengths.ToArray(),weaknesses=a.Weaknesses.ToArray()}).ToList(),
            training=training.Assignments.Select(a=>new TrainingAssignment{PersonId=a.PersonId,Ability=a.Ability,Fee=a.Fee,Completed=a.Completed}).ToList()};
    }
    public string SnapshotJson()=>JsonUtility.ToJson(Capture(),true);
    public bool TryParse(string json,out CampaignSaveData data,out string reason)
    {
        data=null;reason="";
        try
        {
            data=JsonUtility.FromJson<CampaignSaveData>(json);var candidate=data;
            if(candidate==null || candidate.version!=formatVersion)throw new FormatException("Unsupported save version.");
            if(candidate.day<1 || candidate.negativeDays<0 || candidate.totalDelivered<0 || candidate.people==null || candidate.people.Count<1 || candidate.people.Count>recruitment.MaximumPeople || candidate.applicants==null || candidate.training==null)throw new FormatException("Missing campaign candidate.");
            _=Decimal(candidate.cash);_=Decimal(candidate.openingCash);if(Decimal(candidate.preparationCosts)<0 || Decimal(candidate.totalRevenue)<0 || Decimal(candidate.bestDailyRevenue)<0)throw new FormatException("Invalid amounts.");
            if(Decimal(candidate.cash)!=Decimal(candidate.openingCash)-Decimal(candidate.preparationCosts))throw new FormatException("Preparation cash does not reconcile.");
            if(!Finite(candidate.reputation) || candidate.reputation<0 || candidate.reputation>reputation.Maximum || !Finite(candidate.promise) || candidate.promise<=0)throw new FormatException("Invalid policy or reputation.");
            var all=candidate.people.Concat(candidate.applicants.Select(a=>a.person)).ToArray();
            if(all.Any(p=>p==null || !p.HasPersistentId || string.IsNullOrWhiteSpace(p.Name) || !Enum.IsDefined(typeof(RecruitArchetype),p.Archetype)) || all.Select(p=>p.Id).Distinct().Count()!=all.Length || all.Select(p=>p.Name).Distinct().Count()!=all.Length)throw new FormatException("Duplicate or invalid courier identities.");
            if(all.Any(p=>Enumerable.Range(0,6).Any(i=>!Finite(p.Ability((CourierAbility)i)) || !Finite(p.Potential((CourierAbility)i)) || !Finite(p.Experience((CourierAbility)i)) || p.Experience((CourierAbility)i)<0)))throw new FormatException("Invalid courier values.");
            if(candidate.people.Any(p=>p.Status!=DeliveryStatus.Idle && p.Status!=DeliveryStatus.Training))throw new FormatException("Only preparation courier states are supported.");
            if(candidate.applicants.Any(a=>a.baselines==null || a.baselines.Length!=5 || a.strengths==null || a.weaknesses==null || a.strengths.Length!=2 || a.weaknesses.Length!=2 || a.strengths.Concat(a.weaknesses).Distinct().Count()!=4 || a.strengths.Concat(a.weaknesses).Any(i=>i<0 || i>4)))throw new FormatException("Invalid applicant traits.");
            if(candidate.training.Select(a=>a.PersonId).Distinct().Count()!=candidate.training.Count || candidate.training.Any(a=>!Enum.IsDefined(typeof(CourierAbility),a.Ability) || a.Completed || Decimal(a.Fee)<0 || !candidate.people.Any(p=>p.Id==a.PersonId && p.Status==DeliveryStatus.Training)))throw new FormatException("Invalid training instructions.");
            if(candidate.training.Sum(a=>Decimal(a.Fee))>Decimal(candidate.preparationCosts))throw new FormatException("Training fees exceed preparation costs.");
            if(candidate.people.Any(p=>p.Status==DeliveryStatus.Training && !candidate.training.Any(a=>a.PersonId==p.Id)))throw new FormatException("Training instruction missing.");
            return true;
        }
        catch(Exception e) when(e is ArgumentException || e is FormatException || e is OverflowException || e is NullReferenceException)
        {reason=e.Message;data=null;return false;}
    }
    private static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
    private bool TryRead(out CampaignSaveData data,out string reason)
    {
        try{return TryParse(File.ReadAllText(SavePath),out data,out reason);}
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){data=null;reason=e.Message;return false;}
    }
    public void SavePreparation()
    {
        if(!AutomaticSavingEnabled || !ready || restoring || AwaitingChoice || clock.Phase!=DayPhase.BeforeOpening)return;
        try
        {
            string path=SavePath,temp=path+".tmp";Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(temp,SnapshotJson());if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){LastMessage="Could not save: "+e.Message;}
    }
    public void DeleteSave()
    {
        if(!AutomaticSavingEnabled)return;
        try {if(File.Exists(SavePath))File.Delete(SavePath);}
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){LastMessage="Could not delete save: "+e.Message;}
    }
    public IDisposable SuppressSaving()=>new SavingScope(this);
    private sealed class SavingScope:IDisposable
    {
        private readonly CampaignSaveManager owner;private readonly bool original;
        public SavingScope(CampaignSaveManager manager){owner=manager;original=manager.AutomaticSavingEnabled;manager.AutomaticSavingEnabled=false;}
        public void Dispose()=>owner.AutomaticSavingEnabled=original;
    }
}
