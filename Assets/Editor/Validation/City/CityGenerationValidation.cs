using System;
using System.Linq;
using UnityEngine;

public static class CityGenerationValidation
{
    /// <summary>Small seed sample of the new topology; no business simulation or normal save writes.</summary>
    public static string RunOrganic()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        CampaignSaveManager.SessionSavingLocked=true;
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var guard=save.SuppressSaving();
        int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("Organic city: "+label);checks++;}
        var settings=new CityGenerationSettings();string previousPositions=null;
        foreach(int seed in new[]{1401,1402,9187})
        {
            var city=CityGenerator.Build(seed,5,settings);var copy=CityGenerator.Build(seed,5,settings);var larger=CityGenerator.Build(seed,12,settings);
            try
            {
                Check(city.version==6,"new version is used");
                Check(Fingerprint(city)==Fingerprint(copy),"same seed reproduces irregular town");
                string positions=string.Join("|",city.map.Nodes.Select(n=>n.Position.ToString("R")));
                if(previousPositions!=null)Check(positions!=previousPositions,"seed changes road and address coordinates");previousPositions=positions;
                Check(city.map.Nodes.All(n=>larger.map.Nodes.Any(o=>o.Id==n.Id && o.Position==n.Position)),"expansion preserves all existing nodes");
                Check(city.map.Edges.All(e=>larger.map.Edges.Any(o=>o.Id==e.Id && o.NodeAId==e.NodeAId && o.NodeBId==e.NodeBId && o.Distance==e.Distance && o.TrafficFactor==e.TrafficFactor && o.RoadType==e.RoadType && o.Bidirectional==e.Bidirectional)),"expansion preserves roads and costs");
                var metrics=CityValidator.Measure(city,settings);Check(metrics.failures.Count==0,"initial topology safety: "+string.Join("/",metrics.failures));
                var fullMetrics=CityValidator.Measure(larger,settings);Check(fullMetrics.failures.Count==0,"expanded topology safety: "+string.Join("/",fullMetrics.failures));
                var homes=city.map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress).ToArray();
                Check(homes.Length==22,"home count maintained");
                Check(homes.Any(n=>n.Position.x>1) && homes.Any(n=>n.Position.x< -1) && homes.Any(n=>n.Position.y>1) && homes.Any(n=>n.Position.y< -1),"destinations surround the shop");
                Check(city.blocks.Select(b=>b.area.width.ToString("F2")+"/"+b.area.height.ToString("F2")).Distinct().Count()>2,"blocks have varied dimensions");
                int Degree(string id)=>city.map.Edges.Count(e=>e.NodeAId==id || e.NodeBId==id);
                Check(city.map.Nodes.Count(n=>n.NodeType==NodeType.Intersection && Degree(n.Id)>=3)>3,"multiple shared junctions replace four isolated spokes");
                Check(city.map.Edges.Any(e=>e.RoadType==RoadType.Backstreet),"neighborhood alleys present");
                Check(city.map.Nodes.Count(n=>n.NodeType==NodeType.Intersection && Mathf.Abs(n.Position.x)>.1f && Mathf.Abs(n.Position.y)>.1f)>4,"intersections extend throughout the town");
                // Main roads should only meet at traversable junctions, not cross visually without connecting.
                var main=city.map.Edges.Where(e=>e.RoadType==RoadType.Main).ToArray();
                var nodes=city.map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);
                float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
                bool crossed=false;
                for(int i=0;i<main.Length;i++)for(int j=i+1;j<main.Length;j++)
                {
                    var a=main[i];var b=main[j];if(a.NodeAId==b.NodeAId || a.NodeAId==b.NodeBId || a.NodeBId==b.NodeAId || a.NodeBId==b.NodeBId)continue;
                    Vector2 p=nodes[a.NodeAId],r=nodes[a.NodeBId]-p,q=nodes[b.NodeAId],v=nodes[b.NodeBId]-q;
                    float denominator=Cross(r,v);if(Mathf.Abs(denominator)<.0001f)continue;
                    float t=Cross(q-p,v)/denominator,u=Cross(q-p,r)/denominator;
                    if(t>.0001f && t<.9999f && u>.0001f && u<.9999f)crossed=true;
                }
                Check(!crossed,"main road crossings are connected");
            }
            finally{UnityEngine.Object.Destroy(city.map);UnityEngine.Object.Destroy(copy.map);UnityEngine.Object.Destroy(larger.map);}
        }
        return $"PASS: {checks} irregular city checks.";
    }
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
        var edited=CityGenerator.Build(72,3,custom,version:5);
        try
        {
            Check(Mathf.Abs(edited.map.Nodes.First(n=>n.Id=="B0_H1").Position.x-(custom.radialFirstBlockDistance+.7f*custom.blockSize))<.001f,"template address positions are data");
        }
        finally
        {
            UnityEngine.Object.Destroy(edited.map);
        }
        var dense=CityGenerator.Build(1401,5,settings,version:5);
        var legacy=CityGenerator.Build(1401,5,settings,version:3);
        var denseLegacy=CityGenerator.Build(1401,5,settings,version:4);
        var tuned=CityGenerator.Build(1401,5,settings,savedHousesPerBlock:3);
        try
        {
            Check(dense.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==22,"new town has four homes per block plus two original homes");
            Check(legacy.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==12,"version three keeps two homes per block");
            Check(tuned.map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==17,"saved Inspector house count is respected");
            Check(legacy.map.Nodes.All(n=>denseLegacy.map.Nodes.Any(d=>d.Id==n.Id && d.Position==n.Position)),"density increase preserves legacy addresses and junctions");
            foreach(var city in new[]{dense,legacy,tuned})
            {
                foreach(string address in city.blocks.SelectMany(block=>block.addresses))
                {
                    Check(RoadPathfinder.FindShortestPath(city.map.Edges,"Store",address,e=>e.Distance)!=null &&
                          RoadPathfinder.FindShortestPath(city.map.Edges,address,"Store",e=>e.Distance)!=null,"each generated home can receive deliveries and return: "+address);
                }
            }
            var store=dense.map.Nodes.Single(n=>n.IsStore).Position;
            Check(store==Vector2.zero,"new town shop is at the origin");
            var homes=dense.map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress).ToArray();
            Check(homes.Any(n=>n.Position.x>store.x+settings.blockSize),"destinations east of shop");
            Check(homes.Any(n=>n.Position.x<store.x-settings.blockSize),"destinations west of shop");
            Check(homes.Any(n=>n.Position.y>store.y+settings.blockSize),"destinations north of shop");
            Check(homes.Any(n=>n.Position.y<store.y-settings.blockSize),"destinations south of shop");
            float Distance(string from,string to)
            {
                var path=RoadPathfinder.FindShortestPath(dense.map.Edges,from,to,e=>e.Distance);
                return path.Sum(id=>dense.map.Edges.First(e=>e.Id==id).Distance);
            }
            float Individual(string a,string b)=>Distance("Store",a)+Distance(a,"Store")+Distance("Store",b)+Distance(b,"Store");
            float Batch(string a,string b)=>Distance("Store",a)+Distance(a,b)+Distance(b,"Store");
            Check(Batch("B1_H1","B1_H2")<Individual("B1_H1","B1_H2"),"nearby homes reward grouping");
            Check(Mathf.Abs(Batch("B1_H1","B3_H1")-Individual("B1_H1","B3_H1"))<.001f,"opposite directions give no travel saving from batching");
            Check(denseLegacy.version==4 && denseLegacy.housesPerBlock==4,"v4 keeps saved four-home density after v5 release");
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
            UnityEngine.Object.Destroy(denseLegacy.map);
            UnityEngine.Object.Destroy(tuned.map);
        }
        return $"PASS: {checks} city generation checks.";
    }
}
