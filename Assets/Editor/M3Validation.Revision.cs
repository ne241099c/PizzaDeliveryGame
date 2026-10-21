using System;
using System.Linq;
using UnityEngine;

public static partial class M3Validation
{
    private static void ValidateRevision(MapManager map, Action<bool,string> check, Action<float,float,string> near)
    {
        var person = new DeliveryPerson("Sora",0.9f,0.4f,1,0.3f,0.4f,0.3f);
        near(map.GetDrivingSpeedFactor(person),1.24f,"M4-2 driving speed factor");
        near(map.GetTravelTime("Store-N1",person),1.25f/1.24f,"M4-2 main-road driving");
        near(map.GetTravelTime("N1-N2-Backstreet",person),1.3f*1.6f/1.24f,"M4-2 backstreet driving and knowledge");
        var ren=new DeliveryPerson("Ren",0.5f,0.3f,2,0.5f,0.9f,0.4f);
        check(map.GetTravelTime("Store-N1",person)<map.GetTravelTime("Store-N1",ren),"M4-2 faster driving wins on main road");
        check(map.GetTravelTime("N1-N2-Backstreet",ren)>map.GetTravelTime("N1-N2-Main",ren),"M4-2 Ren avoids backstreet");
        var orders=new[]{new Order("Goal1","Test",0,60)};
        var suggested=RoadPathfinder.Suggest(map,orders,person);
        var route=new ManualDeliveryRoute(map);route.TryReplace(suggested.EdgeIds);
        near(suggested.TotalTime,route.GetTime(person),"M4-2 suggestion and manual timing agree");
        near(route.BuildCompletion(orders,person).TotalTime,route.GetTime(person),"M4-2 plan uses six-ability person");
        var trip=new DeliveryTrip(map,route.BuildCompletion(orders,person),person,0,5);
        near(trip.ReturnsAt,route.GetTime(person)*5,"M4-2 actual trip and prediction agree");
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People;
        check(roster.Count==4,"M4-2 initial roster has four people");
        check(roster.Select(p=>p.Name).SequenceEqual(new[]{"Minato","Aoi","Ren","Sora"}),"M4-2 initial names");
        near(roster[0].Driving,0.8f,"M4-2 Minato driving");
        near(roster[1].Service,0.8f,"M4-2 Aoi service");
        near(roster[2].Endurance,0.9f,"M4-2 Ren endurance");
        near(roster[3].Safety,0.3f,"M4-2 Sora safety");
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        near(spawner.DemandFactor(30),1.624505f,"M4-3 30 second demand");
        near(spawner.DemandFactor(180),0.463463f,"M4-3 180 second demand");
        near(spawner.DemandFactor(1),2,"M4-3 demand upper clamp");
        near(spawner.DemandFactor(10000),0.4f,"M4-3 demand lower clamp");
        near(spawner.ShortestArrivalSeconds("Goal1"),3.05f/1.3f*5,"M4-3 fastest one-way Goal1");
        near(spawner.MinimumPromiseSeconds,3.25f/1.3f*5*1.1f,"M4-3 farthest one-way lower bound");
        near(spawner.ReputationFactor,1,"M4-6 initial reputation gives factor one");
        float previousPromise=spawner.PromiseSeconds;
        try
        {
            spawner.SetPromiseSeconds(30);var existing=spawner.CreateOrder("Goal1",12);
            near(existing.Deadline,42,"M4-3 deadline at birth");
            near(spawner.ExpectedInterval,25/Mathf.Pow(2,0.7f),"M4-3 short promise raises frequency");
            spawner.SetPromiseSeconds(180);
            near(existing.Deadline,42,"M4-3 slider cannot rewrite an existing deadline");
            near(spawner.CreateOrder("Goal2",13).Deadline,193,"M4-3 only new orders get new promise");
            near(spawner.ExpectedInterval,25/Mathf.Pow(1f/3f,0.7f),"M4-3 long promise reduces frequency");
            spawner.SetPromiseSeconds(0);near(spawner.PromiseSeconds,spawner.MinimumPromiseSeconds,"M4-3 physical lower bound enforced");
        }
        finally {spawner.SetPromiseSeconds(previousPromise);}
        check(roster[0].Capacity==1 && !roster[0].CanCarry(2),"M4-4 Minato cannot carry two");
        check(roster[1].Capacity==2 && roster[1].CanCarry(2),"M4-4 Aoi can carry two");
        check(roster[0].CanCarry(1),"M4-4 Minato single delivery");
        check(!roster[1].CanCarry(0),"M4-4 empty dispatch rejected");
        var batchOrders=new[]{new Order("Goal1","A",0,60),new Order("Goal2","B",0,60)};
        var batchRoute=new ManualDeliveryRoute(map);batchRoute.TryReplace(RoadPathfinder.Suggest(map,batchOrders,roster[0]).EdgeIds);
        var batchPlan=batchRoute.BuildCompletion(batchOrders,roster[0]);
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        check(!flow.CanDispatch(batchPlan,roster[0]),"M4-4 dispatch guard rejects overload");
        check(flow.DispatchRejectionReason(batchPlan,roster[0]).Contains("can carry 1"),"M4-4 overload reason is visible");
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
            rep.RecordCancellation(1);near(rep.Value,46,"M4-6 cancellation lowers reputation");
            check(rep.DemandFactor<1,"M4-6 damaged reputation reduces demand");
            rep.RecordCancellation(100);near(rep.Value,0,"M4-6 reputation floor");near(rep.DemandFactor,0.6f,"M4-6 lowest demand factor");
            for(int i=0;i<150;i++)rep.RecordArrival(0,0);
            near(rep.Value,100,"M4-6 reputation ceiling");near(rep.DemandFactor,1.4f,"M4-6 highest demand factor");
        }
        finally {UnityEngine.Object.DestroyImmediate(reputationFixture);}



    }
}
