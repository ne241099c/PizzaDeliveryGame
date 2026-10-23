using System;
using System.Linq;
using UnityEngine;

public static class CityCampaignValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
        var save=Find<CampaignSaveManager>();
        using var scope=save.SuppressSaving();
        var campaign=Find<CampaignManager>();
        var map=Find<MapManager>();
        var clock=Find<GameClock>();
        var spawner=Find<OrderSpawner>();
        var roster=Find<CourierRoster>();
        var original=map.SeedOverride;bool legacy=map.UseSampleForValidation;int checks=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("City campaign: "+name);checks++;}
        try
        {
            map.UseSampleForValidation=false;map.SeedOverride=1401;save.StartNewGame();clock.AutomaticTick=false;spawner.enabled=false;
            Check(map.City!=null && map.City.blocks.Count==5 && map.Nodes.Count(n=>n.NodeType==NodeType.DeliveryAddress)==12 && map.City.metrics.failures.Count==0,"new game opens the configured larger safe town");
            save.StartNewGame();
            Check(save.TryParse(save.SnapshotJson(),out _,out _),"restarting the same seed refreshes saved stage");
            var first=CityGenerationValidation.Fingerprint(map.City);
            var ids=map.Edges.Select(e=>e.Id).ToArray();
            var data=save.Capture();Check(data.city!=null && save.TryParse(JsonUtility.ToJson(data),out _,out _),"seed and stage save validates");
            map.SeedOverride=1402;save.StartNewGame();Check(CityGenerationValidation.Fingerprint(map.City)!=first,"new seed changes town");
            campaign.RestorePreparation(data);Check(CityGenerationValidation.Fingerprint(map.City)==first,"Continue rebuilds exact saved town");
            float knowledge=roster.People[0].LocalKnowledge;
            foreach(int day in new[]{6,11,21})
            {
                data.day=day;data.city.day=day;data.city.blocks=map.CitySettings.Blocks(day);campaign.RestorePreparation(data);
                Check(map.City.blocks.Count==map.CitySettings.Blocks(day) && ids.All(id=>map.Edges.Any(e=>e.Id==id)),"day expansion retains roads "+day);
                Check(roster.People[0].LocalKnowledge==knowledge,"knowledge survives expansion "+day);
                string f=CityGenerationValidation.Fingerprint(map.City);
                campaign.RestorePreparation(data);
                Check(CityGenerationValidation.Fingerprint(map.City)==f,"same day and seed reproduce construction "+day);
                Check(map.City.metrics.failures.Count==0,"daily traffic remains safe "+day);
            }
            data.day=1;data.city.day=1;data.city.version=1;data.city.blocks=map.CitySettings.BlocksForVersion(1,1);
            Check(save.TryParse(JsonUtility.ToJson(data),out var oldTown,out _),"version one town remains a valid save");
            campaign.RestorePreparation(oldTown);
            Check(map.City.blocks.Count==3 && map.CaptureCity().version==1,"Continue retains three-block legacy town and version");
            data.city.version=2;
            Check(save.TryParse(JsonUtility.ToJson(data),out var interimTown,out _),"interim version two three-block save remains usable");
            campaign.RestorePreparation(interimTown);
            Check(map.City.blocks.Count==3 && map.CaptureCity().version==2,"Continue retains interim version two town without rewriting file");
            data.city=null;
            Check(save.TryParse(JsonUtility.ToJson(data),out var legacyData,out _),"legacy null city JSON round trip");
            campaign.RestorePreparation(legacyData);
            Check(map.City==null && map.Nodes.Count==5,"old save retains original town");
            return $"PASS: {checks} city campaign and save checks.";
        }
        finally
        {
            map.UseSampleForValidation=legacy;
            map.SeedOverride=original;
            save.StartNewGame();
            clock.AutomaticTick=false;
            CampaignSaveManager.SessionSavingLocked=true;
        }
    }
}
