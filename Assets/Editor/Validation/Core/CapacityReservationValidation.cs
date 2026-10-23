using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Focused three-box compatibility, cooking reservations and complete route-search checks.</summary>
public static class CapacityReservationValidation
{
    private static T Find<T>() where T:Object=>Object.FindFirstObjectByType<T>();
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        CampaignSaveManager.SessionSavingLocked=true;
        var save=Find<CampaignSaveManager>();using var suppress=save.SuppressSaving();
        var clock=Find<GameClock>();var spawner=Find<OrderSpawner>();var campaign=Find<CampaignManager>();
        var roster=Find<CourierRoster>();var book=Find<OrderBook>();var map=Find<MapManager>();
        int checks=0;
        void Check(bool ok,string label){if(!ok)throw new Exception("Three-box reservation: "+label);checks++;}
        try
        {
            clock.AutomaticTick=false;spawner.enabled=false;campaign.NewGame();
            var aoi=roster.People[0];
            Check(aoi.Capacity==DeliveryPerson.MaximumCapacity && aoi.CanCarry(3),"initial Aoi carries three boxes");
            Check(!aoi.CanCarry(4) && !aoi.CanCarry(0),"structural capacity limits");
            var minato=new DeliveryPerson("Capacity Minato",.5f,.5f,1,.5f,.5f,.5f);
            Check(minato.Capacity==1 && !minato.CanCarry(2),"one-box profile is preserved");
            var learner=new DeliveryPerson("Capacity Learner",.5f,.5f,2,.5f,.5f,.5f);
            roster.AddPerson(learner);
            Check(Find<TrainingManager>().Rejection(learner,CourierAbility.Capacity)=="","two-box courier may train toward three");
            Find<CourierProgression>().ApplyTraining(learner,CourierAbility.Capacity,1);
            Check(learner.Capacity==3,"capacity training reaches three");
            learner.Capacity=4;Check(learner.Capacity==3,"training cannot exceed structural maximum");
            Check(save.TryParse(save.SnapshotJson(),out var saved,out var reason),"new save parses: "+reason);
            Check(saved.people.First(p=>p.Id==aoi.Id).Capacity==3,"three-box save round trip");
            var legacy=saved.people.First(p=>p.Id==aoi.Id);legacy.Capacity=2;legacy.SetPotential(CourierAbility.Capacity,2);
            Check(save.TryParse(JsonUtility.ToJson(saved),out var old,out reason),"old one/two-box profile still parses: "+reason);
            Check(old.people.First(p=>p.Id==aoi.Id).Capacity==2,"loading legacy data retains earned capacity");
            roster.RestoreRoster(old.people);
            Check(roster.People.First(p=>p.Id==aoi.Id).Capacity==2 && roster.People.All(p=>p.Potential(CourierAbility.Capacity)==3),"legacy roster unlocks third-box training without granting capacity");
            campaign.NewGame();aoi=roster.People[0];Find<KitchenCrew>().Restore(Array.Empty<KitchenStaff>());campaign.StartBusiness();
            string[] homes=map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress).Select(n=>n.Id).Take(3).ToArray();
            Check(homes.Length==3,"three distinct delivery destinations are available");
            var raw=homes.Select((h,i)=>new Order(h,"Reserved "+i,0,10000)).ToArray();
            foreach(var order in raw)book.AddOrder(order);
            Check(raw.All(book.CanReserve) && raw.All(o=>!book.CanSelect(o)),"unbaked tickets can be reserved but are not ready");
            Check(book.SelectForDispatch(raw) && book.SelectedOrders.Count==3,"three cooking tickets can be planned together");
            Check(!book.SelectForDispatch(new[]{raw[0],raw[0]}),"duplicate reservations are rejected");
            var fourth=new Order(homes[0],"Fourth",0,10000);book.AddOrder(fourth);
            Check(!book.SelectForDispatch(raw.Concat(new[]{fourth})),"four-ticket selection is rejected");
            var expired=new Order(homes[0],"Expired",0,1);book.AddOrder(expired);
            clock.Advance(Find<ScoreManager>().CancellationAt(expired)+.01f);
            Check(!book.CanReserve(expired),"expired tickets cannot be reserved");
            var suggestion=RoadPathfinder.Suggest(map,raw,aoi);
            Check(suggestion!=null,"three-destination route suggestion exists");
            var route=new ManualDeliveryRoute(map);
            Check(route.TryReplace(suggestion.EdgeIds),"suggested road sequence is connected");
            var plan=route.BuildCompletion(raw,aoi);
            Check(plan!=null && plan.Orders.Count==3 && raw.All(o=>route.HasVisited(o.DestinationNodeId)),"manual route includes all three stops and returns");
            float best=float.PositiveInfinity;
            for(int i=0;i<3;i++)for(int j=0;j<3;j++)for(int k=0;k<3;k++)
            {
                if(i==j || j==k || i==k)continue;
                string[] path={map.StoreNodeId,homes[i],homes[j],homes[k],map.StoreNodeId};float total=0;
                for(int leg=0;leg<4;leg++)
                {
                    var edges=RoadPathfinder.FindShortestPath(map.Edges,path[leg],path[leg+1],e=>map.GetTravelTime(e.Id,aoi));
                    Check(edges!=null,"reachable permutation "+i+j+k+" leg "+leg);
                    total+=edges.Sum(id=>map.GetTravelTime(id,aoi));
                }
                best=Mathf.Min(best,total);
            }
            Check(Mathf.Abs(suggestion.TotalTime-best)<.001f,"suggestion minimizes all six stop permutations");
            Check(Find<DeliveryFlowController>().DispatchRejectionReason(plan,aoi).Contains("cooking"),"reservation alone never permits departure");
            book.RemoveOrders(raw);
            var ready=homes.Select((h,i)=>new Order(h,"Packed "+i,clock.ElapsedSeconds,10000,BakingState.Ready)).ToArray();
            foreach(var order in ready)book.AddOrder(order);
            Check(book.SelectForDispatch(ready) && roster.SelectForDispatch(aoi),"packed three-box load remains selectable");
            var packedSuggestion=RoadPathfinder.Suggest(map,ready,aoi);route.TryReplace(packedSuggestion.EdgeIds);
            var packedPlan=route.BuildCompletion(ready,aoi);
            Check(Find<DeliveryFlowController>().CanDispatch(packedPlan,aoi),"three packed pizzas may depart");
            Check(RoadPathfinder.Suggest(map,ready.Concat(new[]{fourth}).ToArray(),aoi)==null,"four-stop suggestion is rejected");
            return $"PASS: {checks} three-box and reservation checks.";
        }
        finally
        {
            campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;
            CampaignSaveManager.SessionSavingLocked=true;
        }
    }
}
