using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Shop scene (S2) rules: handover bags, kitchen staff, workbenches and the version 3 save. Play Mode only.</summary>
public static class ShopValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Shop Scene (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Shop: "+name);count++;}
        var save=Find<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=Find<CampaignManager>();var clock=Find<GameClock>();var list=Find<OrderListUI>();var cook=Find<CookingManager>();var spawner=Find<OrderSpawner>();
        var economy=Find<BusinessEconomy>();var roster=Find<DeliveryPersonSelector>();var handover=Find<HandoverManager>();var planner=Find<DeliveryPlannerUI>();
        var crew=Find<KitchenCrew>();var recruitment=Find<RecruitmentManager>();var facilities=Find<FacilityManager>();var flow=Find<DeliveryFlowController>();
        bool tick=clock.AutomaticTick,spawning=spawner.enabled;clock.AutomaticTick=false;
        try
        {
            // Generated control references must participate in Unity serialization (script reload).
            var business=Find<BusinessView>();
            if(business!=null)
            {
                var controls=new SerializedObject(business);
                var tabs=controls.FindProperty("zoneTabs");
                Check(tabs!=null && tabs.arraySize==3 && Enumerable.Range(0,3).All(i=>tabs.GetArrayElementAtIndex(i).objectReferenceValue!=null),"zone controls survive Unity serialization");
                var steps=controls.FindProperty("steps");
                Check(steps!=null && steps.arraySize==4 && Enumerable.Range(0,4).All(i=>steps.GetArrayElementAtIndex(i).objectReferenceValue!=null),"dispatch steps survive Unity serialization");
                var previous=business.Zone;
                foreach(ShopZone zone in Enum.GetValues(typeof(ShopZone)))
                {
                    business.ShowZone(zone,true);
                    var middle=(StickerButton)tabs.GetArrayElementAtIndex(1).objectReferenceValue;
                    Check(business.Zone==zone && middle.gameObject.activeSelf && middle.GetComponent<CanvasGroup>().alpha==(zone==ShopZone.Dispatch?0:1),"zone switch uses CanvasGroup "+zone);
                }
                business.ShowZone(previous,true);
                var hub=Find<HubView>();var hubControls=new SerializedObject(hub);
                foreach(string name in new[]{"tabs","tabBadges"})
                {
                    var values=hubControls.FindProperty(name);
                    Check(values!=null && values.arraySize==4 && Enumerable.Range(0,name=="tabs"?4:2).All(i=>values.GetArrayElementAtIndex(i).objectReferenceValue!=null),"hub controls survive Unity serialization "+name);
                }
            }
            // ---- handover bags ----
            campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();
            var aoi=roster.People[0];var ren=roster.People[1];
            var raw=new Order("Goal1","Raw",0,100);list.AddOrder(raw);
            Check(handover.BagRejection(raw,aoi).Contains("梱包") && !handover.Bag(raw,aoi),"only packed boxes go into a bag");
            var r1=M7Validation.ReadyOrder("Goal1","Box 1",0,100);var r2=M7Validation.ReadyOrder("Goal2","Box 2",0,100);var r3=M7Validation.ReadyOrder("Goal2","Box 3",0,100);
            list.AddOrder(r1);list.AddOrder(r2);list.AddOrder(r3);int rack=cook.RackCount;
            Check(handover.Bag(r1,aoi) && handover.Bag(r2,aoi) && handover.IsBagged(r1) && handover.Holder(r2)==aoi,"two boxes fill Aoi's bag (load 2)");
            Check(!handover.Bag(r3,aoi) && handover.LastMessage.Contains("いっぱい"),"a full bag rejects the next box with a reason");
            Check(cook.RackCount==rack-2 && handover.RackOrders.SequenceEqual(new[]{r3}),"bagged boxes leave the counter rack");
            Check(roster.SelectedPerson==aoi && list.SelectedOrders.SequenceEqual(new[]{r1,r2}),"the bag becomes the dispatch selection");
            Check(handover.Unbag(r2) && list.SelectedOrders.SequenceEqual(new[]{r1}) && !handover.IsBagged(r2),"taking a box out updates the selection");
            Check(handover.Bag(r3,ren) && handover.Select(aoi) && list.SelectedOrders.SequenceEqual(new[]{r1}) && roster.SelectedPerson==aoi,"switching couriers selects their own bag");
            planner.DispatchWithSuggestion();Check(aoi.CurrentTrip!=null && handover.BagOf(aoi).Count==0 && handover.BagOf(ren).Count==1,"dispatch empties only the departing courier's bag");
            Check(handover.BagRejection(r2,aoi).Length>0,"a travelling courier cannot take boxes");
            var shortBox=M7Validation.ReadyOrder("Goal1","Short",0,5);list.AddOrder(shortBox);Check(handover.Bag(shortBox,ren),"second box for Ren");
            clock.Advance(5.01f);Check(!list.Contains(shortBox) && !handover.IsBagged(shortBox) && handover.IsBagged(r3),"expired boxes leave the bag");
            clock.Advance(clock.BusinessDuration);Check(handover.BagOf(ren).Count==0,"closing clears all bags");
            clock.Advance(1000);

            // ---- kitchen staff ----
            campaign.NewGame();
            Check(crew.Count==1 && crew.People[0].Name=="Mio" && Mathf.Approximately(crew.People[0].Speed,.4f) && recruitment.TeamCount==3,"start with Aoi, Ren and the apprentice Mio");
            Check(economy.DailyWage(crew.People[0])==80,"kitchen wage 40 + 50 x (speed + care)");
            var funding=new SerializedObject(economy);float initial=funding.FindProperty("initialCash").floatValue;funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=initial;funding.ApplyModifiedPropertiesWithoutUndo();
            int guard=0;while(recruitment.KitchenApplicants.Count==0 && guard++<20)recruitment.GenerateDay();
            Check(recruitment.KitchenApplicants.Count==1,"kitchen candidates appear in the applicant pool");
            var candidate=recruitment.KitchenApplicants[0];
            Check(!roster.People.Any(p=>p.Name==candidate.Staff.Name) && !recruitment.Applicants.Any(a=>a.Person.Name==candidate.Staff.Name) && candidate.Staff.Name!="Mio","kitchen candidate names are unique");
            decimal cash=economy.Cash,fee=recruitment.HiringCost(candidate.Staff);
            Check(fee==economy.DailyWage(candidate.Staff)*3 && recruitment.Hire(candidate) && crew.Count==2 && economy.Cash==cash-fee,"hiring kitchen staff costs three wage days");
            Check(recruitment.TeamCount==4 && recruitment.HireRejection(recruitment.Applicants.FirstOrDefault()).Contains("limit"),"kitchen staff count against the shared team limit");
            Check(facilities.Purchase(FacilityType.Workbench) && facilities.Workbenches==3 && facilities.Maintenance==15,"workbench purchase 400 adds a bench and maintenance");
            Check(facilities.Price(FacilityType.Workbench)==700,"second workbench costs 700");
            string json=save.SnapshotJson();Check(save.TryParse(json,out var data,out var reason),"version 3 save validates "+reason);
            Check(data.version==3 && data.crew.Count==2 && data.facilities.workbench==1,"crew and workbenches saved");
            var hiredName=candidate.Staff.Name;campaign.NewGame();Check(crew.Count==1 && facilities.Workbenches==2,"new game resets kitchen and benches");
            campaign.RestorePreparation(data);Check(crew.Count==2 && crew.People.Any(c=>c.Name==hiredName) && facilities.Workbenches==3,"restore brings back kitchen staff and benches");
            var duplicate=save.Capture();duplicate.crew.Add(new KitchenStaff(roster.People[0].Name,.5f,.5f));Check(!save.TryParse(JsonUtility.ToJson(duplicate),out _,out _),"duplicate names across couriers and kitchen staff are rejected");
            var overflow=save.Capture();overflow.crew.Add(new KitchenStaff("Extra",.5f,.5f));Check(!save.TryParse(JsonUtility.ToJson(overflow),out _,out _),"saved team cannot exceed the shared limit");
            campaign.StartBusiness();Check(cook.BenchCount==3,"bench count applies from the next opening");
            spawner.enabled=false;clock.Advance(clock.BusinessDuration+1000);
            Check(economy.Ledger.Wages==economy.TeamWages && economy.KitchenWages==crew.People.Sum(c=>economy.DailyWage(c)),"both kitchen staff are paid at settlement");
            campaign.NewGame();
            var staff=crew.People[0];decimal severance=recruitment.SeveranceCost(staff);cash=economy.Cash;
            Check(recruitment.Fire(staff) && crew.Count==0 && economy.Cash==cash-severance,"dismissing kitchen staff pays one wage day");
            campaign.StartBusiness();spawner.enabled=false;var alone=new Order("Goal1","No staff",0,100);list.AddOrder(alone);clock.Advance(20);
            Check(alone.Baking==BakingState.Waiting,"without kitchen staff tickets wait for the player");
            clock.Advance(1000);campaign.NewGame();
        }
        finally{clock.AutomaticTick=tick;spawner.enabled=spawning;}
        return $"PASS: {count} shop checks.";
    }
}
