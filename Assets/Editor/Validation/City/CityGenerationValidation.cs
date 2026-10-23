using System;
using System.Linq;
using UnityEngine;

public static class CityGenerationValidation
{
    public static string Fingerprint(GeneratedCity city)=>string.Join("|",city.map.Nodes.Select(n=>$"{n.Id}:{n.Position.x:R},{n.Position.y:R}"))+"/"+string.Join("|",city.map.Edges.Select(e=>$"{e.Id}:{e.NodeAId}:{e.NodeBId}:{e.Distance:R}:{e.TrafficFactor:R}:{e.Bidirectional}"))+"/"+string.Join("|",city.blocks.Select(b=>b.template.kind));
    public static string RunBasic()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("City: "+label);checks++;}
        var settings=new CityGenerationSettings();
        foreach(int seed in new[]{1401,1402,9187})
        {
            var a=CityGenerator.Build(seed,3,settings);var b=CityGenerator.Build(seed,3,settings);var larger=CityGenerator.Build(seed,5,settings);
            try
            {
                Check(Fingerprint(a)==Fingerprint(b),"same seed reproduces topology and templates");
                Check(a.map.Nodes.All(n=>larger.map.Nodes.Any(o=>n.Id==o.Id && n.Position==o.Position)),"existing nodes stay fixed when expanding");
                Check(a.map.Edges.All(e=>larger.map.Edges.Any(o=>o.Id==e.Id && o.NodeAId==e.NodeAId && o.NodeBId==e.NodeBId && o.Distance==e.Distance && o.TrafficFactor==e.TrafficFactor)),"existing road IDs and costs stay fixed");
                var metrics=CityValidator.Measure(a,settings);Check(metrics.failures.Count==0,string.Join(",",metrics.failures));
            }
            finally{UnityEngine.Object.Destroy(a.map);UnityEngine.Object.Destroy(b.map);UnityEngine.Object.Destroy(larger.map);}
        }
        var custom=new CityGenerationSettings();
        custom.templates.First(t=>t.kind==DistrictKind.Park).addressPositions[0]=new Vector2(.7f,-.3f);
        var edited=CityGenerator.Build(72,3,custom);
        try
        {
            Check(Mathf.Abs(edited.map.Nodes.First(n=>n.Id=="B0_H1").Position.x-(7+.7f*custom.blockSize))<.001f,"template address positions are data");
        }
        finally
        {
            UnityEngine.Object.Destroy(edited.map);
        }
        var dense=CityGenerator.Build(1401,5,settings);
        var legacy=CityGenerator.Build(1401,5,settings,version:3);
        var tuned=CityGenerator.Build(1401,5,settings,savedHousesPerBlock:3);
        try
        {
            Check(dense.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==22,"new town has four homes per block plus two original homes");
            Check(legacy.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==12,"version three keeps two homes per block");
            Check(tuned.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==17,"saved Inspector house count is respected");
            Check(legacy.map.Nodes.All(n=>dense.map.Nodes.Any(d=>d.Id==n.Id && d.Position==n.Position)),"density increase preserves legacy addresses and junctions");
            foreach(var city in new[]{dense,legacy,tuned})
            {
                foreach(string address in city.blocks.SelectMany(block=>block.addresses))
                {
                    Check(RoadPathfinder.FindShortestPath(city.map.Edges,"Store",address,e=>e.Distance)!=null &&
                          RoadPathfinder.FindShortestPath(city.map.Edges,address,"Store",e=>e.Distance)!=null,"each generated home can receive deliveries and return: "+address);
                }
            }
            var road=new MapEdge("test","a","b",10,RoadType.Backstreet,1);
            var points=RoadGeometry.Points(road,Vector2.zero,new Vector2(4,0),.85f);
            Check(points.Length==4 && points[0]==Vector2.zero && points[3]==new Vector2(4,0),"backstreet remains attached at both junctions");
            Check(points[1].y>.5f && points[2].y>.5f,"backstreet middle is visibly separate from main road");
            Check(RoadGeometry.PointAt(points,0)==points[0] && RoadGeometry.PointAt(points,1)==points[3],"travel on display geometry starts and ends at the junctions");
            Check(Vector2.Distance(RoadGeometry.PointAt(points,.5f),new Vector2(2,.85f))<.001f,"trip display follows the distinct backstreet");
            Check(road.Distance==10,"display geometry does not change delivery distance");
        }
        finally
        {
            UnityEngine.Object.Destroy(dense.map);
            UnityEngine.Object.Destroy(legacy.map);
            UnityEngine.Object.Destroy(tuned.map);
        }
        return $"PASS: {checks} city generation checks.";
    }
}
