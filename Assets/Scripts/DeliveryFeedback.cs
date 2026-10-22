using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Observes resolved trips and existing counters. Never writes scores, cash or clock time.</summary>
[DefaultExecutionOrder(1500)]
public sealed class DeliveryFeedback : MonoBehaviour
{
    [SerializeField] private bool animationsEnabled=true;
    [SerializeField] private AnimationCurve rise=AnimationCurve.EaseInOut(0,0,1,1);
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private GameClock clock;
    [SerializeField] private MapManager map;
    [SerializeField] private ScoreManager score;
    [SerializeField] private CookingManager cooking;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private SettlementScreenUI settlement;
    [SerializeField] private ScreenRouter router;
    [SerializeField] private TMP_Text cashLabel,reputationLabel,settlementLabel;
    [SerializeField] private RectTransform courierContent;
    [SerializeField] private Image timerFill;
    private readonly List<DeliveryTrip> trips=new();
    private readonly HashSet<string> shown=new();
    private decimal previousCash,cashDelta;
    private float previousReputation,reputationDelta,cashUntil,reputationUntil;
    private int previousPage=-1;
    private static DeliveryFeedback active;
    public bool AnimationsEnabled {get=>animationsEnabled;set=>animationsEnabled=value;}
    public static bool Animate=>active!=null && active.animationsEnabled && !UIThemeDriver.RenderingSuspended;
    private void Awake(){active=this;previousCash=economy.Cash;previousReputation=reputation.Value;}
    private void OnEnable(){flow.Dispatched+=Dispatched;flow.TripsCleared+=Clear;clock.OnDayStarted+=Clear;clock.Advanced+=Arrivals;}
    private void OnDisable(){flow.Dispatched-=Dispatched;flow.TripsCleared-=Clear;clock.OnDayStarted-=Clear;clock.Advanced-=Arrivals;}
    private void OnDestroy(){if(active==this)active=null;}
    private void Clear(){trips.Clear();shown.Clear();previousPage=-1;}
    private void Dispatched(DeliveryTrip trip)=>trips.Add(trip);
    private void Arrivals(float before,float now)
    {
        foreach(var trip in trips)foreach(var delivery in trip.Deliveries.Where(d=>d.Resolved))
        {
            if(!shown.Add(delivery.Order.Id) || !Animate)continue;
            var node=map.Nodes.First(n=>n.Id==delivery.Order.DestinationNodeId);
            bool late=delivery.ArrivesAt>delivery.Order.Deadline;
            int points=score.ScoreArrival(delivery.ArrivesAt,delivery.Order.Deadline,trip.ServiceAtDispatch)+cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt);
            int tip=score.TipForArrival(delivery.ArrivesAt,delivery.Order.Deadline,trip.ServiceAtDispatch);
            int penalty=score.ScorePerDelivery-score.ScoreArrival(delivery.ArrivesAt,delivery.Order.Deadline,trip.ServiceAtDispatch);
            string caption=(late?"遅れ ":"配達完了 ")+points.ToString("+0;-0;0");
            if(tip>0)caption+="\n<color=#"+ColorUtility.ToHtmlStringRGB(UITheme.Active.money)+">+チップ "+tip+"</color>";
            if(late)caption+="\n減点 -"+Mathf.Max(0,penalty);
            if(cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt)>0)caption+="\n"+new string('★',delivery.Order.Stars)+" +"+cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt);
            StartCoroutine(Float(caption,node.Position,late?UITheme.Active.bad:UITheme.Active.good));
        }
    }
    private IEnumerator Float(string caption,Vector2 position,Color color)
    {
        var go=new GameObject("DeliveryFeedbackText");go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshPro>();text.font=UITheme.Active.font;text.fontSize=UITheme.Active.worldLabelSize;text.color=color;text.text=caption;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(3,2);go.GetComponent<MeshRenderer>().sortingOrder=50;
        for(float time=0;time<UITheme.Active.effectSeconds;time+=Time.unscaledDeltaTime)
        {
            if(!Animate)break;float progress=time/UITheme.Active.effectSeconds;go.transform.position=(Vector3)position+Vector3.up*rise.Evaluate(progress)*UITheme.Active.effectRise;var tint=color;tint.a=1-progress;text.color=tint;yield return null;
        }
        Destroy(go);
    }
    private void LateUpdate()=>Render();
    public void Render()
    {
        var theme=UITheme.Active;
        if(economy.Cash!=previousCash){cashDelta=economy.Cash-previousCash;previousCash=economy.Cash;cashUntil=Time.unscaledTime+theme.effectSeconds;}
        if(reputation.Value!=previousReputation){reputationDelta=reputation.Value-previousReputation;previousReputation=reputation.Value;reputationUntil=Time.unscaledTime+theme.effectSeconds;}
        if(Animate && Time.unscaledTime<cashUntil){cashLabel.text+=(cashDelta>0?" ▲":" ▼")+cashDelta.ToString("+0.00;-0.00;0.00");cashLabel.color=cashDelta>0?theme.good:theme.bad;}
        if(Animate && Time.unscaledTime<reputationUntil){reputationLabel.text+=(reputationDelta>0?" ▲":" ▼")+reputationDelta.ToString("+0.0;-0.0;0.0");reputationLabel.color=reputationDelta>0?theme.good:theme.bad;}
        float timeFraction=clock.RemainingSeconds/clock.BusinessDuration;timerFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(timeFraction),1);timerFill.color=timeFraction<=theme.closingWarning?theme.bad:theme.good;
        var buttons=courierContent.GetComponentsInChildren<Button>(true).Where(b=>b.transform.parent==courierContent).ToArray();
        for(int i=0;i<Mathf.Min(buttons.Length,roster.People.Count);i++)
        {
            var bar=buttons[i].transform.Find("StaminaBar/Fill");if(bar!=null)bar.GetComponent<Image>().color=roster.People[i].CurrentStamina<=theme.lowStamina?theme.bad:theme.good;
            if(roster.People[i].CurrentStamina<=theme.lowStamina){var label=buttons[i].GetComponentInChildren<TMP_Text>();if(!label.text.Contains("体力注意"))label.text+="\n! 体力注意";}
        }
        if(router.Current==ScreenId.Settlement && settlement.Page!=previousPage){previousPage=settlement.Page;if(Animate && !settlement.IsShowingAll && (previousPage==0 || previousPage==2))StartCoroutine(CountUp(previousPage));}
    }
    private IEnumerator CountUp(int page)
    {
        string original=settlementLabel.text;var number=Regex.Match(original,@"[+-]?\d+\.\d{2}");if(!number.Success)yield break;
        decimal target=decimal.Parse(number.Value,System.Globalization.CultureInfo.InvariantCulture);
        for(float time=0;time<UITheme.Active.effectSeconds;time+=Time.unscaledDeltaTime)
        {
            if(!Animate || settlement.Page!=page || settlement.IsShowingAll)yield break;
            decimal value=target*(decimal)rise.Evaluate(time/UITheme.Active.effectSeconds);settlementLabel.text=original.Substring(0,number.Index)+value.ToString("F2")+original.Substring(number.Index+number.Length);yield return null;
        }
        if(settlement.Page==page && !settlement.IsShowingAll)settlementLabel.text=original;
    }
}
