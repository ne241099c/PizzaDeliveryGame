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
        return $"PASS: {checks} city generation checks.";
    }
}
