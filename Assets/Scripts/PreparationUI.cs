using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PreparationUI : MonoBehaviour
{
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
    private void OnEnable() => recruitment.Changed+=Refresh;
    private void OnDisable() => recruitment.Changed-=Refresh;
    private void Start() => Refresh();
    private void Update()
    {
        overview.text=$"Day {campaign.DayNumber} / Cash {economy.Cash:F2} / Reputation {reputation.Value:F1} / Team {roster.People.Count}/{recruitment.MaximumPeople}";
        message.text=recruitment.LastMessage;
    }
    private void FitPool(List<GameObject> rows,int count,Transform parent)
    {
        while(rows.Count<count)rows.Add(Instantiate(rowTemplate,parent));
        for(int i=0;i<rows.Count;i++)rows[i].SetActive(i<count);
    }
    private string Profile(DeliveryPerson p) => $"{p.Name} / Wage {economy.DailyWage(p):F2}\n"
        +$"Drive {p.Driving:F2}  Know {p.LocalKnowledge:F2}  Load {p.Capacity}\n"
        +$"Service {p.Service:F2}  Endurance {p.Endurance:F2}  Safety {p.Safety:F2}";
    public void Refresh()
    {
        FitPool(teamRows,roster.People.Count,teamContent);FitPool(applicantRows,recruitment.Applicants.Count,applicantContent);
        for(int i=0;i<roster.People.Count;i++)
        {
            var person=roster.People[i];var row=teamRows[i];
            row.transform.Find("Profile").GetComponent<TextMeshProUGUI>().text=Profile(person)+$"\nSeverance: {recruitment.SeveranceCost(person):F2}";
            var button=row.GetComponentInChildren<Button>();button.GetComponentInChildren<TextMeshProUGUI>().text="Fire";
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>recruitment.Fire(person));
        }
        for(int i=0;i<recruitment.Applicants.Count;i++)
        {
            var applicant=recruitment.Applicants[i];var row=applicantRows[i];
            row.transform.Find("Profile").GetComponent<TextMeshProUGUI>().text=Profile(applicant.Person)+$"\nHiring: {recruitment.HiringCost(applicant.Person):F2}";
            var button=row.GetComponentInChildren<Button>();button.GetComponentInChildren<TextMeshProUGUI>().text="Hire";
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>recruitment.Hire(applicant));
        }
    }
}
