using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>Floating "配達完了 +120" text over each delivered address, and the shared switch that turns presentation
/// animations off. Observes resolved trips only; never writes scores, cash or clock time.</summary>
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
    private readonly List<DeliveryTrip> trips=new();
    private readonly HashSet<string> shown=new();
    private static DeliveryFeedback active;
    public bool AnimationsEnabled {get=>animationsEnabled;set=>animationsEnabled=value;}
    public static bool Animate=>active!=null && active.animationsEnabled && !UiRendering.Suspended;
    private void Awake()=>active=this;
    private void OnEnable(){flow.Dispatched+=Dispatched;flow.TripsCleared+=Clear;clock.OnDayStarted+=Clear;clock.Advanced+=Arrivals;}
    private void OnDisable(){flow.Dispatched-=Dispatched;flow.TripsCleared-=Clear;clock.OnDayStarted-=Clear;clock.Advanced-=Arrivals;}
    private void OnDestroy(){if(active==this)active=null;}
    private void Clear(){trips.Clear();shown.Clear();}
    private void Dispatched(DeliveryTrip trip)=>trips.Add(trip);
    private void Arrivals(float before,float now)
    {
        foreach(var trip in trips)foreach(var delivery in trip.Deliveries.Where(d=>d.Resolved))
        {
            if(!shown.Add(delivery.Order.Id) || !Animate)continue;
            var node=map.Nodes.First(n=>n.Id==delivery.Order.DestinationNodeId);
            bool late=delivery.ArrivesAt>delivery.Order.Deadline;
            int points=score.ScoreArrival(delivery.Order,delivery.ArrivesAt,trip.ServiceAtDispatch)+cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt);
            int tip=score.TipForArrival(delivery.Order,delivery.ArrivesAt,trip.ServiceAtDispatch);
            int penalty=score.BasePoints(delivery.Order)-score.ScoreArrival(delivery.Order,delivery.ArrivesAt,trip.ServiceAtDispatch);
            string caption=(late?"遅れ ":"配達完了 ")+points.ToString("+0;-0;0");
            if(tip>0)caption+="\n<color=#"+ColorUtility.ToHtmlStringRGB(UITheme.Active.money)+">+チップ "+tip+"</color>";
            if(late)caption+="\n減点 -"+Mathf.Max(0,penalty);
            if(cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt)>0)caption+="\n"+new string('★',delivery.Order.Stars)+" +"+cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt);
            StartCoroutine(Float(caption,node.Position,late?UITheme.Active.bad:UITheme.Active.good));
        }
    }
    private IEnumerator Float(string caption,Vector2 position,Color color)
    {
        var go=new GameObject("DeliveryFeedbackText");
        go.transform.SetParent(transform,false);
        var text=go.AddComponent<TextMeshPro>();
        text.font=UITheme.Active.font;
        text.fontSize=UITheme.Active.worldLabelSize;
        text.color=color;
        text.text=caption;
        text.alignment=TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta=new Vector2(3,2);
        go.GetComponent<MeshRenderer>().sortingOrder=50;
        for(float time=0;time<UITheme.Active.effectSeconds;time+=Time.unscaledDeltaTime)
        {
            if(!Animate)break;
            float progress=time/UITheme.Active.effectSeconds;
            go.transform.position=(Vector3)position+Vector3.up*rise.Evaluate(progress)*UITheme.Active.effectRise;
            var tint=color;
            tint.a=1-progress;
            text.color=tint;
            yield return null;
        }
        Destroy(go);
    }
}
