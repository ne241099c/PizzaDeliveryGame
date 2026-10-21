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
    }
}
