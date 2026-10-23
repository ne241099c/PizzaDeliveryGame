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
    public FacilitySaveData facilities;
    public CitySaveState city;
    public string facilityPurchaseCosts;
    public int version,day,totalDelivered,totalScore,negativeDays;
    public string cash,openingCash,preparationCosts,totalRevenue,bestDailyRevenue;
    public float reputation,promise;
    public List<DeliveryPerson> people;
    public List<ApplicantSaveData> applicants;
    public List<TrainingAssignment> training;
    public List<KitchenStaff> crew;
    public List<KitchenStaff> kitchenApplicants;
}
/// <summary>Only preparation snapshots are saved. Simulations/tests suppress all normal file changes.</summary>
/// <summary>Best results over every game played (kept when a new game starts or the shop closes).</summary>
[Serializable] public sealed class BestRecords
{
    public int bestDay,bestDelivered;public string bestDailyRevenue="0",bestTotalRevenue="0";
}
public sealed class CampaignSaveManager : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private GameClock clock;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private RecruitmentManager recruitment;
    [SerializeField] private TrainingManager training;
    [SerializeField] private OrderSpawner spawner;
    [SerializeField] private KitchenCrew crew;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button continueButton,newButton;
    // choiceText is optional: the title screen (TitleView) shows the save state itself.
    [SerializeField] private TextMeshProUGUI notice,choiceText;
    [SerializeField] private string filename="campaign.json";
    [SerializeField,Min(1)] private int formatVersion=3;
    private bool ready,restoring,listenersRegistered;
    public bool AutomaticSavingEnabled {get;set;}=true;
    public string SavePathOverride {get;set;}
    public string SavePath=>SavePathOverride??Path.Combine(SaveDirectory,filename);
    /// <summary>The data folder under the fixed GameInfo.SaveFolder rather than productName, so renaming the game keeps the saves.
    /// On desktop that is the sibling of persistentDataPath; elsewhere the platform folder itself.</summary>
    public static string SaveDirectory
    {
        get
        {
            string data=Application.persistentDataPath;
            #if UNITY_EDITOR || UNITY_STANDALONE
            string company=Path.GetDirectoryName(data);if(!string.IsNullOrEmpty(company))return Path.Combine(company,GameInfo.SaveFolder);
            #endif
            return data;
        }
    }
    public bool AwaitingChoice {get;private set;}
    public string LastMessage {get;private set;}="";
    public void ClearNotice()=>LastMessage="";
    private void OnEnable()
    {campaign.PreparationEntered+=SavePreparation;recruitment.Changed+=SavePreparation;if(crew!=null)crew.Changed+=SavePreparation;training.Changed+=SavePreparation;spawner.PolicyChanged+=SavePreparation;if(facilities!=null)facilities.Changed+=SavePreparation;}
    private void OnDisable()
    {campaign.PreparationEntered-=SavePreparation;recruitment.Changed-=SavePreparation;if(crew!=null)crew.Changed-=SavePreparation;training.Changed-=SavePreparation;spawner.PolicyChanged-=SavePreparation;if(facilities!=null)facilities.Changed-=SavePreparation;}
    private void Update() {if(notice!=null)notice.text=LastMessage;}
    public static decimal Decimal(string value)=>decimal.Parse(value,NumberStyles.Number,CultureInfo.InvariantCulture);
    public bool HasUsableSave => TryRead(out _,out _);
    /// <summary>No save file at all (the first launch): the title highlights the tutorial.</summary>
    public bool IsFirstLaunch => !File.Exists(SavePath);
    /// <summary>The usable save in one short line for the continue button, e.g. "3日目 ・ ¥1,240 ・ 評判 72". Empty without one.</summary>
    public string ContinueSummary => TryRead(out var data,out _)?$"{data.day}日目 ・ {UiKit.Yen(Decimal(data.cash))} ・ 評判 {data.reputation:F0}":"";
    /// <summary>Why the save cannot be used (unreadable file), or empty.</summary>
    public string SaveProblem => File.Exists(SavePath) && !TryRead(out _,out var reason)?"セーブを読めませんでした（"+reason+"）。最初から始めます。":"";
    public string TitleDescription
    {
        get
        {
            if(TryRead(out var data,out var reason))return $"続きから：{data.day}日目の開店準備 ・ 所持金 {UiKit.Yen(Decimal(data.cash))} ・ 評判 {data.reputation:F0}";
            return File.Exists(SavePath)?"セーブを読めませんでした（"+reason+"）。最初から始めます。":"セーブはまだありません。最初から始めましょう。";
        }
    }

    // ---- best records (records.json next to the campaign save) ----
    public string RecordsPath=>Path.Combine(Path.GetDirectoryName(SavePath),"records.json");
    private BestRecords records;
    public BestRecords Records
    {
        get
        {
            if(records!=null)return records;
            try{records=File.Exists(RecordsPath)?JsonUtility.FromJson<BestRecords>(File.ReadAllText(RecordsPath)):null;}
            catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){records=null;}
            return records??=new BestRecords();
        }
    }
    /// <summary>Records broken by the last settled day (for the settlement and game-over screens).</summary>
    public IReadOnlyList<string> NewRecords=>newRecords;
    private readonly List<string> newRecords=new();
    public string RecordsSummary
    {
        get
        {
            var r=Records;if(r.bestDay==0)return "最高記録：まだありません";
            return $"最高記録：{r.bestDay}日目まで ・ 1日の売上 {UiKit.Yen(Decimal(r.bestDailyRevenue))} ・ 通算売上 {UiKit.Yen(Decimal(r.bestTotalRevenue))} ・ 配達 {r.bestDelivered}件";
        }
    }
    /// <summary>Called when a day is settled. Writes only while normal saving is enabled (validations suppress it).</summary>
    public void RecordDay(int day,decimal dailyRevenue,decimal totalRevenue,int totalDelivered)
    {
        newRecords.Clear();var r=Records;
        if(day>r.bestDay){r.bestDay=day;newRecords.Add("到達日数");}
        if(dailyRevenue>Decimal(r.bestDailyRevenue)){r.bestDailyRevenue=dailyRevenue.ToString(CultureInfo.InvariantCulture);newRecords.Add("1日の売上");}
        if(totalRevenue>Decimal(r.bestTotalRevenue)){r.bestTotalRevenue=totalRevenue.ToString(CultureInfo.InvariantCulture);newRecords.Add("通算売上");}
        if(totalDelivered>r.bestDelivered){r.bestDelivered=totalDelivered;newRecords.Add("配達数");}
        if(!AutomaticSavingEnabled){records=null;return;} // suppressed: forget the in-memory change too
        if(newRecords.Count==0)return;
        try
        {
            string path=RecordsPath,temp=path+".tmp";Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(temp,JsonUtility.ToJson(r,true));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){LastMessage="Could not save records: "+e.Message;}
    }
    public void Startup()
    {
        if(!listenersRegistered){continueButton.onClick.AddListener(Continue);newButton.onClick.AddListener(StartNewGame);listenersRegistered=true;}
        var router=FindFirstObjectByType<ScreenRouter>();
        if(router!=null && router.StartAtTitle)
        {ready=false;AwaitingChoice=true;UIVisibility.Set(choicePanel,true);if(choiceText!=null)choiceText.text=TitleDescription;return;}
        if(File.Exists(SavePath) && TryRead(out var data,out var reason))
        {ready=false;AwaitingChoice=true;UIVisibility.Set(choicePanel,true);if(choiceText!=null)choiceText.text=$"CONTINUE CAMPAIGN?\nDay {data.day} / Cash {data.cash} / Reputation {data.reputation:F1}";return;}
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
        return new CampaignSaveData{city=FindFirstObjectByType<MapManager>().CaptureCity(),facilities=facilities.Capture(),facilityPurchaseCosts=economy.Ledger.FacilityPurchases.ToString(CultureInfo.InvariantCulture),version=formatVersion,day=campaign.DayNumber,totalDelivered=campaign.TotalDelivered,totalScore=campaign.TotalScore,negativeDays=campaign.ConsecutiveNegativeDays,
            cash=economy.Cash.ToString(CultureInfo.InvariantCulture),openingCash=economy.OpeningCash.ToString(CultureInfo.InvariantCulture),preparationCosts=economy.Ledger.PreparationCosts.ToString(CultureInfo.InvariantCulture),
            totalRevenue=campaign.TotalRevenue.ToString(CultureInfo.InvariantCulture),bestDailyRevenue=campaign.BestDailyRevenue.ToString(CultureInfo.InvariantCulture),reputation=reputation.Value,promise=spawner.PromiseSeconds,
            people=roster.People.Select(p=>p.Clone()).ToList(),
            applicants=recruitment.Applicants.Select(a=>new ApplicantSaveData{person=a.Person.Clone(),baselines=a.Baselines.ToArray(),strengths=a.Strengths.ToArray(),weaknesses=a.Weaknesses.ToArray()}).ToList(),
            training=training.Assignments.Select(a=>new TrainingAssignment{PersonId=a.PersonId,Ability=a.Ability,Fee=a.Fee,Completed=a.Completed}).ToList(),
            crew=crew!=null?crew.People.Select(c=>c.Clone()).ToList():new List<KitchenStaff>(),kitchenApplicants=recruitment.KitchenApplicants.Select(k=>k.Staff.Clone()).ToList()};
    }
    public string SnapshotJson()=>JsonUtility.ToJson(Capture(),true);
    public bool TryParse(string json,out CampaignSaveData data,out string reason)
    {
        data=null;reason="";
        try
        {
            data=JsonUtility.FromJson<CampaignSaveData>(json);var candidate=data;
            // Version 1 had no equipment. Normalize in memory without overwriting the original file.
            if(candidate!=null && candidate.version==1){candidate.version=2;candidate.facilities=new FacilitySaveData();candidate.facilityPurchaseCosts="0";}
            // Version 2 had no kitchen staff or workbenches. The starting apprentice joins independently of headcount.
            if(candidate!=null && candidate.version==2 && formatVersion==3)
            {
                candidate.version=3;candidate.kitchenApplicants=new List<KitchenStaff>();candidate.crew=new List<KitchenStaff>();
                if(candidate.people!=null && candidate.facilities!=null)candidate.crew.Add(new KitchenStaff("Mio",.4f,.4f));
            }
            if(candidate==null || candidate.version!=formatVersion)throw new FormatException("Unsupported save version.");
            if(!FindFirstObjectByType<MapManager>().ValidCity(candidate.city,candidate.day))throw new FormatException("Invalid city state.");
            if(!facilities.Valid(candidate.facilities))throw new FormatException("Invalid facility levels.");
            if(candidate.day<1 || candidate.negativeDays<0 || candidate.totalDelivered<0 || candidate.people==null || candidate.people.Count<1 || candidate.applicants==null || candidate.training==null || candidate.crew==null || candidate.kitchenApplicants==null)throw new FormatException("Missing campaign candidate.");
            var kitchen=candidate.crew.Concat(candidate.kitchenApplicants).ToArray();
            if(kitchen.Any(k=>k==null || string.IsNullOrWhiteSpace(k.Name) || !Finite(k.Speed) || !Finite(k.Care)) || kitchen.Select(k=>k.Name).Concat(candidate.people.Select(p=>p.Name)).Distinct().Count()!=kitchen.Length+candidate.people.Count)throw new FormatException("Duplicate or invalid kitchen staff.");
            if(Decimal(candidate.facilityPurchaseCosts)<0 || Decimal(candidate.facilityPurchaseCosts)>Decimal(candidate.preparationCosts))throw new FormatException("Invalid facility purchases.");
            _=Decimal(candidate.cash);_=Decimal(candidate.openingCash);if(Decimal(candidate.preparationCosts)<0 || Decimal(candidate.totalRevenue)<0 || Decimal(candidate.bestDailyRevenue)<0)throw new FormatException("Invalid amounts.");
            if(Decimal(candidate.cash)!=Decimal(candidate.openingCash)-Decimal(candidate.preparationCosts))throw new FormatException("Preparation cash does not reconcile.");
            if(!Finite(candidate.reputation) || candidate.reputation<0 || candidate.reputation>reputation.Maximum || !Finite(candidate.promise) || candidate.promise<=0)throw new FormatException("Invalid policy or reputation.");
            var all=candidate.people.Concat(candidate.applicants.Select(a=>a.person)).ToArray();
            if(all.Any(p=>p==null || !p.HasPersistentId || string.IsNullOrWhiteSpace(p.Name) || !Enum.IsDefined(typeof(RecruitArchetype),p.Archetype)) || all.Select(p=>p.Id).Distinct().Count()!=all.Length || all.Select(p=>p.Name).Distinct().Count()!=all.Length)throw new FormatException("Duplicate or invalid courier identities.");
            if(all.Any(p=>Enumerable.Range(0,6).Any(i=>!Finite(p.Ability((CourierAbility)i)) || !Finite(p.Potential((CourierAbility)i)) || !Finite(p.Experience((CourierAbility)i)) || p.Experience((CourierAbility)i)<0)))throw new FormatException("Invalid courier values.");
            if(candidate.people.Any(p=>p.Status!=DeliveryStatus.Idle && p.Status!=DeliveryStatus.Training))throw new FormatException("Only preparation courier states are supported.");
            if(candidate.applicants.Any(a=>a.baselines==null || a.baselines.Length!=5 || a.strengths==null || a.weaknesses==null || a.strengths.Length!=2 || a.weaknesses.Length!=2 || a.strengths.Concat(a.weaknesses).Distinct().Count()!=4 || a.strengths.Concat(a.weaknesses).Any(i=>i<0 || i>4)))throw new FormatException("Invalid applicant traits.");
            if(candidate.training.Select(a=>a.PersonId).Distinct().Count()!=candidate.training.Count || candidate.training.Any(a=>!Enum.IsDefined(typeof(CourierAbility),a.Ability) || a.Completed || Decimal(a.Fee)<0 || !candidate.people.Any(p=>p.Id==a.PersonId && p.Status==DeliveryStatus.Training)))throw new FormatException("Invalid training instructions.");
            if(candidate.training.Sum(a=>Decimal(a.Fee))+Decimal(candidate.facilityPurchaseCosts)>Decimal(candidate.preparationCosts))throw new FormatException("Training fees exceed preparation costs.");
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
