using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PreparationUI : MonoBehaviour
{
    [SerializeField] private TrainingManager training;
    private string displayMessage="";
    [SerializeField] private RecruitmentManager recruitment;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private TextMeshProUGUI overview;
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private Transform teamContent;
    [SerializeField] private Transform applicantContent;
    [SerializeField] private GameObject rowTemplate;
    private readonly List<GameObject> teamRows=new(),applicantRows=new();
    private void OnEnable() {recruitment.Changed+=RecruitmentChanged;training.Changed+=TrainingChanged;Refresh();}
    private void OnDisable() {recruitment.Changed-=RecruitmentChanged;training.Changed-=TrainingChanged;}
    private void RecruitmentChanged(){displayMessage=recruitment.LastMessage;Refresh();}
    private void TrainingChanged(){displayMessage=training.LastMessage;Refresh();}
    private void Start() => Refresh();
    private void Update()
    {
        overview.text=$"Day {campaign.DayNumber} / Cash {economy.Cash:F2} / Reputation {reputation.Value:F1} / Team {roster.People.Count}/{recruitment.MaximumPeople}";
        message.text=displayMessage;
    }
    private void FitPool(List<GameObject> rows,int count,Transform parent)
    {
        while(rows.Count<count)rows.Add(Instantiate(rowTemplate,parent));
        for(int i=0;i<rows.Count;i++)rows[i].SetActive(i<count);
    }
    private string Profile(DeliveryPerson p) => $"{p.Name} / {p.Archetype} / Wage {economy.DailyWage(p):F2}\n"
        +$"Drive {p.Driving:F2}  Know {p.LocalKnowledge:F2}  Load {p.Capacity}\n"
        +$"Service {p.Service:F2}  Endurance {p.Endurance:F2}  Safety {p.Safety:F2}";
    public void Refresh()
    {
        FitPool(teamRows,roster.People.Count,teamContent);FitPool(applicantRows,recruitment.Applicants.Count,applicantContent);
        for(int i=0;i<roster.People.Count;i++)
        {
            var person=roster.People[i];var row=teamRows[i];row.GetComponent<CourierAbilitiesView>()?.Show(person);
            row.transform.Find("Profile").GetComponent<TextMeshProUGUI>().text=Profile(person)+$"\nSeverance: {recruitment.SeveranceCost(person):F2}";
            var button=row.GetComponentInChildren<Button>();button.GetComponentInChildren<TextMeshProUGUI>().text="Fire";
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>recruitment.Fire(person));
            var controls=row.transform.Find("TrainingControls");
            for(int a=0;a<6;a++)
            {
                var ability=(CourierAbility)a;var train=controls.Find("Train"+a).GetComponent<Button>();
                train.GetComponentInChildren<TextMeshProUGUI>().text=$"Train {ability}\n{training.Cost(person,ability):F2}";
                train.interactable=!training.HasAssignment(person) && person.Ability(ability)<person.Potential(ability);
                train.onClick.RemoveAllListeners();train.onClick.AddListener(()=>training.Request(person,ability));
            }
            controls.gameObject.SetActive(true);
            var cancel=row.transform.Find("CancelTraining").GetComponent<Button>();cancel.gameObject.SetActive(training.HasAssignment(person));cancel.onClick.RemoveAllListeners();cancel.onClick.AddListener(()=>training.Cancel(person));
        }
        for(int i=0;i<recruitment.Applicants.Count;i++)
        {
            var applicant=recruitment.Applicants[i];var row=applicantRows[i];row.transform.Find("TrainingControls").gameObject.SetActive(false);row.transform.Find("CancelTraining").gameObject.SetActive(false);row.GetComponent<CourierAbilitiesView>()?.Show(applicant.Person);
            row.transform.Find("Profile").GetComponent<TextMeshProUGUI>().text=Profile(applicant.Person)+$"\nHire: {recruitment.HiringCost(applicant.Person):F2} / Strong: "+string.Join("/",applicant.Strengths.Select(i=>((CourierAbility)i).ToString()));
            var button=row.GetComponentInChildren<Button>();button.GetComponentInChildren<TextMeshProUGUI>().text="Hire";
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>recruitment.Hire(applicant));
        }
    }
}
