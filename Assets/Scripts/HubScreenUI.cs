using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HubScreenUI : MonoBehaviour
{
    [SerializeField] private ScreenRouter router;
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private GameClock clock;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private CanvasGroup[] tabs;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private TextMeshProUGUI forecast, history;
    [SerializeField] private RectTransform policy, businessPolicySlot;
    private readonly List<string> records=new();
    public int SelectedTab {get;private set;}
    private void OnEnable(){clock.OnDayEnded+=RecordDay;campaign.PreparationEntered+=Preparation;router.Changed+=ScreenChanged;}
    private void OnDisable(){clock.OnDayEnded-=RecordDay;campaign.PreparationEntered-=Preparation;router.Changed-=ScreenChanged;}
    private void Start(){for(int i=0;i<tabButtons.Length;i++){int tab=i;tabButtons[i].onClick.AddListener(()=>ShowTab(tab));}ShowTab(0);ScreenChanged(router.Current);}
    private void Preparation(){if(campaign.DayNumber==1)records.Clear();ShowTab(0);}
    private void RecordDay(){records.Add($"Day {campaign.DayNumber} / Profit {economy.Ledger.Profit:F2} / Cash {economy.Cash:F2}");}
    public void ShowTab(int tab){SelectedTab=tab;for(int i=0;i<tabs.Length;i++)UIVisibility.Set(tabs[i].gameObject,i==tab);}
    private void ScreenChanged(ScreenId screen)
    {
        policy.SetParent(screen==ScreenId.Business?businessPolicySlot:tabs[2].transform,false);
        policy.anchorMin=policy.anchorMax=policy.pivot=new Vector2(0,1);policy.anchoredPosition=Vector2.zero;policy.sizeDelta=new Vector2(1280,100);
    }
    private void Update()
    {
        decimal wages=roster.People.Sum(economy.DailyWage),fixedCost=economy.FixedCost(campaign.DayNumber);
        forecast.text=$"Today / fixed {fixedCost:F2} + wages {wages:F2} / cash {economy.Cash:F2}";
        history.text="PREVIOUS DAYS\n"+string.Join("\n",records)+$"\nTotal sales {campaign.TotalRevenue:F2} / Best day {campaign.BestDailyRevenue:F2}";
    }
}
