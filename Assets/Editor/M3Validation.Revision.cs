using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static partial class M3Validation
{
    private static void ValidateRevision(MapManager map, Action<bool,string> check, Action<float,float,string> near)
    {
        var person = new DeliveryPerson("Sora",0.9f,0.4f,1,0.3f,0.4f,0.3f);
        near(map.GetDrivingSpeedFactor(person),1.24f,"M4-2 driving speed factor");
        near(map.GetTravelTime("Store-N1",person),1.25f/1.24f,"M4-2 main-road driving");
        near(map.GetTravelTime("N1-N2-Backstreet",person),1.1f*1.6f/1.24f,"M4-2 backstreet driving and knowledge");
        var ren=new DeliveryPerson("Ren",0.5f,0.3f,2,0.5f,0.9f,0.4f);
        check(map.GetTravelTime("Store-N1",person)<map.GetTravelTime("Store-N1",ren),"M4-2 faster driving wins on main road");
        check(map.GetTravelTime("N1-N2-Backstreet",ren)>map.GetTravelTime("N1-N2-Main",ren),"M4-2 Ren avoids backstreet");
        var orders=new[]{M7Validation.ReadyOrder("Goal1","Test",0,60)};
        var suggested=RoadPathfinder.Suggest(map,orders,person);
        var route=new ManualDeliveryRoute(map);route.TryReplace(suggested.EdgeIds);
        near(suggested.TotalTime,route.GetTime(person),"M4-2 suggestion and manual timing agree");
        near(route.BuildCompletion(orders,person).TotalTime,route.GetTime(person),"M4-2 plan uses six-ability person");
        var trip=new DeliveryTrip(map,route.BuildCompletion(orders,person),person,0,5);
        near(trip.ReturnsAt,route.GetTime(person)*5,"M4-2 actual trip and prediction agree");
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People;
        check(roster.Count==2,"M5 initial roster has two people");
        check(roster.Select(p=>p.Name).SequenceEqual(new[]{"Aoi","Ren"}),"M5 initial names");
        near(roster[0].Driving,0.6f,"M5 Aoi driving");
        near(roster[0].Service,0.8f,"M5 Aoi service");
        near(roster[1].Endurance,0.9f,"M5 Ren endurance");
        near(roster[1].Safety,0.4f,"M5 Ren safety");
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        near(spawner.DemandFactor(30),1.624505f,"M4-3 30 second demand");
        near(spawner.DemandFactor(180),0.463463f,"M4-3 180 second demand");
        near(spawner.DemandFactor(1),2,"M4-3 demand upper clamp");
        near(spawner.DemandFactor(10000),0.4f,"M4-3 demand lower clamp");
        near(spawner.ShortestArrivalSeconds("Goal1"),2.85f/1.3f*5,"M4-3 fastest one-way Goal1");
        near(spawner.MinimumPromiseSeconds,2*3.05f/1.3f*5*1.1f+12,"M7 round-trip lower bound plus cooking");
        near(spawner.ReputationFactor,1,"M4-6 initial reputation gives factor one");
        float previousPromise=spawner.PromiseSeconds;
        try
        {
            spawner.SetPromiseSeconds(30);var existing=spawner.CreateOrder("Goal1",12);
            near(existing.Deadline,12+spawner.PromiseSeconds,"M7 deadline snapshots physically clamped promise");
            near(spawner.ExpectedInterval,25/spawner.DemandFactor(spawner.PromiseSeconds),"M7 clamped short promise controls frequency");
            spawner.SetPromiseSeconds(180);
            near(existing.Deadline,12+Mathf.Max(30,spawner.MinimumPromiseSeconds),"M7 slider cannot rewrite an existing deadline");
            near(spawner.CreateOrder("Goal2",13).Deadline,193,"M4-3 only new orders get new promise");
            near(spawner.ExpectedInterval,25/Mathf.Pow(1f/3f,0.7f),"M4-3 long promise reduces frequency");
            spawner.SetPromiseSeconds(0);near(spawner.PromiseSeconds,spawner.MinimumPromiseSeconds,"M4-3 physical lower bound enforced");
        }
        finally {spawner.SetPromiseSeconds(previousPromise);}
        var minato=new DeliveryPerson("Minato",.8f,.9f,1,.4f,.6f,.7f);
        check(minato.Capacity==1 && !minato.CanCarry(2),"M4-4 Minato cannot carry two");
        check(roster[0].Capacity==2 && roster[0].CanCarry(2),"M4-4 Aoi can carry two");
        check(roster[0].CanCarry(1),"M4-4 Minato single delivery");
        check(!roster[1].CanCarry(0),"M4-4 empty dispatch rejected");
        var batchOrders=new[]{M7Validation.ReadyOrder("Goal1","A",0,60),M7Validation.ReadyOrder("Goal2","B",0,60)};
        var batchRoute=new ManualDeliveryRoute(map);batchRoute.TryReplace(RoadPathfinder.Suggest(map,batchOrders,minato).EdgeIds);
        var batchPlan=batchRoute.BuildCompletion(batchOrders,minato);
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        check(!flow.CanDispatch(batchPlan,minato),"M4-4 dispatch guard rejects overload");
        string capacityReason=flow.DispatchRejectionReason(batchPlan,minato);
        bool storeOpen=UnityEngine.Object.FindFirstObjectByType<GameClock>().Phase==DayPhase.Open;
        check(capacityReason.Contains(storeOpen ? "can carry 1" : "closed"),"M5 dispatch reason respects preparation/open phase");
        near(DeliveryScoring.Calculate(60,60,100,5,0.8f,0.2f),116,"M4-6 Aoi on-time tip");
        near(DeliveryScoring.Calculate(60,60,100,5,0.4f,0.2f),108,"M4-6 Minato on-time tip");
        near(DeliveryScoring.Calculate(72,60,100,5,1,0.2f),40,"M4-6 late delivery has no tip");
        near(DeliveryScoring.Calculate(90,60,100,5,1,0.2f),0,"M4-6 zero floor remains");
        var reputationFixture=new GameObject("Isolated reputation validation");reputationFixture.SetActive(false);
        try
        {
            var rep=reputationFixture.AddComponent<ReputationManager>();rep.ResetForDay();
            near(rep.Value,50,"M4-6 initial reputation");near(rep.DemandFactor,1,"M4-6 neutral reputation factor");
            near(rep.ArrivalChange(10,0),-2,"M4-6 late reputation formula");
            near(rep.ArrivalChange(10,1),-1,"M4-6 service protects reputation");
            near(rep.ArrivalChange(100,0),-5,"M4-6 late loss ceiling");
            rep.RecordArrival(0,0);near(rep.Value,51,"M4-6 on-time reputation gain");
            rep.RecordCancellation(3,true);near(rep.Value,51,"M5-0 closing cancellation preserves reputation");
            rep.RecordCancellation(1);near(rep.Value,46,"M4-6 cancellation lowers reputation");
            check(rep.DemandFactor<1,"M4-6 damaged reputation reduces demand");
            rep.RecordCancellation(100);near(rep.Value,0,"M4-6 reputation floor");near(rep.DemandFactor,0.6f,"M4-6 lowest demand factor");
            for(int i=0;i<150;i++)rep.RecordArrival(0,0);
            near(rep.Value,100,"M4-6 reputation ceiling");near(rep.DemandFactor,1.4f,"M4-6 highest demand factor");
        }
        finally {UnityEngine.Object.DestroyImmediate(reputationFixture);}
        var dynamicsFixture=new GameObject("Isolated dynamics validation");dynamicsFixture.SetActive(false);
        try
        {
            var rules=dynamicsFixture.AddComponent<DeliveryDynamics>();
            near(rules.StaminaAfterTrip(1,0.6f,30),0.892f,"M4-7 trip depletion formula");
            near(rules.StaminaAfterRest(0.5f,0.6f,30),0.599f,"M4-7 rest recovery formula");
            near(rules.StaminaAfterTrip(0.1f,0,1000),0,"M4-7 stamina floor");
            near(rules.StaminaAfterRest(0.9f,1,1000),1,"M4-7 stamina ceiling");
            near(rules.AccidentProbability(0.3f,0,RoadType.Backstreet),0.105f,"M4-7 tired Sora backstreet risk");
            near(rules.AccidentProbability(0.3f,1,RoadType.Main),0.035f,"M4-7 fresh Sora main risk");
            near(rules.AccidentProbability(1,0,RoadType.Backstreet),0,"M4-7 perfect safety zero risk");
            check(rules.AccidentProbability(0.3f,0,RoadType.Backstreet)>rules.AccidentProbability(0.8f,1,RoadType.Main),"M4-7 safety and rest reduce risk");
            rules.ApplyTripFatigue(person,1,person.Endurance,0.5f/(0.004f*(1.5f-person.Endurance)));
            near(map.GetStaminaSpeedFactor(person),0.85f,"M4-7 fatigue speed factor");
            float tiredTime=route.GetTime(person);
            var frozen=new DeliveryTrip(map,route.BuildCompletion(orders,person),person,0,5);
            rules.RecoverStamina(person,10000);
            check(route.GetTime(person)<tiredTime,"M4-7 resting makes next route faster");
            near(frozen.ReturnsAt,tiredTime*5,"M4-7 dispatched speed remains frozen");
            var clean=new DeliveryTrip(map,route.BuildCompletion(orders,person),person,0,5,rules,()=>1d);
            clean.AdvanceAccidents(10000);check(clean.AccidentCount==0,"M4-7 fixed high draws cause no accidents");
            var stopped=new DeliveryTrip(map,route.BuildCompletion(orders,person),person,0,5,rules,()=>0d);
            float baseReturn=stopped.ReturnsAt,baseArrival=stopped.Deliveries[0].ArrivesAt;
            stopped.AdvanceAccidents(0);
            check(stopped.AccidentCount==1,"M4-7 draw on entry exactly once");
            near(stopped.ReturnsAt,baseReturn+15,"M4-7 stop delays return");
            near(stopped.Deliveries[0].ArrivesAt,baseArrival+15,"M4-7 stop delays arrival");
            check(stopped.IsStoppedAt(5) && !stopped.IsStoppedAt(15),"M4-7 exact pause interval");
            near(Vector2.Distance(stopped.PositionAt(5),map.Nodes.First(n=>n.IsStore).Position),0,"M4-7 stationary during stop");
            stopped.AdvanceAccidents(1);check(stopped.AccidentCount==1,"M4-7 no duplicate draw on later frame");
            stopped.AdvanceAccidents(10000);
            check(stopped.AccidentCount==stopped.EdgeIds.Count,"M4-7 repeated road traversals each get a draw");
            near(stopped.ReturnsAt,baseReturn+15*stopped.EdgeIds.Count,"M4-7 multiple stop delays accumulate");
            near(stopped.Deliveries[0].ArrivesAt,baseArrival+15*route.NodeIds.ToList().IndexOf("Goal1"),"M4-7 future return accidents do not change an earlier arrival");
            near(stopped.TotalDelaySeconds,15*stopped.EdgeIds.Count,"M4-7 total accident delay");
            check(DeliveryMarkerView.MarkerCaption(stopped,5).Contains("STOP 10s"),"M4-5 map marker shows stop countdown");
            check(!DeliveryMarkerView.MarkerCaption(stopped,stopped.ReturnsAt+1).Contains("STOP"),"M4-5 map stop display clears");
            float firstRoadDuration=map.GetTravelTime("Store-N1",person)*5;
            var midpoint=Vector2.Lerp(map.Nodes.First(n=>n.IsStore).Position,map.Nodes.First(n=>n.Id=="N1").Position,0.5f);
            near(Vector2.Distance(stopped.PositionAt(15+firstRoadDuration/2),midpoint),0,"M4-5 movement resumes after pause");
            check(Vector2.Distance(clean.PositionAt(2),stopped.PositionAt(2))>0,"M4-5 one stopped courier does not stop another");
            near(Vector2.Distance(stopped.PositionAt(stopped.ReturnsAt+1),map.Nodes.First(n=>n.IsStore).Position),0,"M4-5 delayed trip finishes at store");
            var other=dynamicsFixture.AddComponent<DeliveryDynamics>();
            foreach(var d in new[]{rules,other}){var settings=new SerializedObject(d);settings.FindProperty("useFixedSeed").boolValue=true;settings.FindProperty("randomSeed").intValue=7;settings.ApplyModifiedPropertiesWithoutUndo();d.ResetForDay();}
            var firstRoll=rules.CreateRoller();var secondRoll=other.CreateRoller();
            check(Enumerable.Range(0,10).All(_=>firstRoll()==secondRoll()),"M4-7 fixed seed reproduces draws");
        }
        finally {UnityEngine.Object.DestroyImmediate(dynamicsFixture);}
        var actualSelector=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var actualSettings=new SerializedObject(actualSelector);
        var actualContent=(RectTransform)actualSettings.FindProperty("deliveryPersonListContent").objectReferenceValue;
        var rosterScroll=actualContent.GetComponentInParent<ScrollRect>();
        var screenRouter=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();bool bottomBand=screenRouter!=null && screenRouter.ImplementationStage>=3;
        check(rosterScroll!=null && (bottomBand ? rosterScroll.horizontal && !rosterScroll.vertical : rosterScroll.vertical && !rosterScroll.horizontal),"M4-0 variable roster scroll follows current screen layout");
        check(rosterScroll.content==actualContent,"M4-0 preserves existing roster content");
        Canvas.ForceUpdateCanvases();
        check(actualContent.rect.height>=0 && rosterScroll.viewport.rect.height>0,"M5 short roster fits scroll viewport");
        var rosterFixture=new GameObject("Variable roster UI validation",typeof(RectTransform));rosterFixture.SetActive(false);
        try
        {
            var selector=rosterFixture.AddComponent<DeliveryPersonSelector>();var settings=new SerializedObject(selector);
            settings.FindProperty("deliveryPersonListContent").objectReferenceValue=rosterFixture.transform;
            settings.FindProperty("deliveryPersonItemPrefab").objectReferenceValue=actualSettings.FindProperty("deliveryPersonItemPrefab").objectReferenceValue;
            var fixtureClock=rosterFixture.AddComponent<GameClock>();fixtureClock.BeginDay();
            settings.FindProperty("clock").objectReferenceValue=fixtureClock;
            var entries=settings.FindProperty("deliveryPeople");entries.arraySize=5;entries.GetArrayElementAtIndex(4).FindPropertyRelative("Name").stringValue="Extra";
            settings.ApplyModifiedPropertiesWithoutUndo();rosterFixture.SetActive(true);selector.RebuildRoster();
            var rows=rosterFixture.GetComponentsInChildren<Button>(true);
            check(rows.Length==5,"M4-0 fifth inspector courier gets a row");
            rows[4].onClick.Invoke();check(selector.SelectedPerson==selector.People[4],"M4-0 fifth courier is selectable");
            string abilities=rows[4].GetComponentInChildren<TextMeshProUGUI>().text;
            check(new[]{"Drive","Know","Load","Service","Endurance","Safety"}.All(abilities.Contains),"M4-0 six abilities shown on extra row");
            check(rows.All(r=>r.transform.Find("StaminaBar/Fill")!=null),"M4-0 every row has a stamina bar");
        }
        finally {UnityEngine.Object.DestroyImmediate(rosterFixture);}
        var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();
        var resultsPanel=(GameObject)new SerializedObject(planner).FindProperty("resultsPanel").objectReferenceValue;
        check(resultsPanel.transform.GetSiblingIndex()==resultsPanel.transform.parent.childCount-1,"M4-8 results cover policy controls");
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        check(canvas.GetComponent<CanvasScaler>().screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,"M4-8 whole layout fits variable aspect ratio");
        var footer=UIVisibility.Find(canvas.transform,"Footer").GetComponent<RectTransform>();var footerCorners=new Vector3[4];footer.GetWorldCorners(footerCorners);
        check(footerCorners.All(p=>p.x>=canvas.pixelRect.xMin-1 && p.x<=canvas.pixelRect.xMax+1 && p.y>=canvas.pixelRect.yMin-1 && p.y<=canvas.pixelRect.yMax+1),"M4-8 dispatch and reset remain inside Game view");
        check(planner.ResultSummary.Contains("Reputation:"),"M4-8 result includes reputation");
        check(planner.ResultSummary.Contains("Aoi / Load 2"),"M4-8 result includes capacity");
        check(planner.ResultSummary.Contains("Stamina"),"M4-8 result includes remaining stamina");
        near(UnityEngine.Object.FindFirstObjectByType<ReputationManager>().Value,50,"M4-8 fresh day reputation reset");
        check(roster.All(p=>p.CurrentStamina==1 && p.IsAvailable && p.CurrentTrip==null),"M4-8 fresh day stamina and availability reset");






    }
}
