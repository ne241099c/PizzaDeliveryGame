using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DistrictKind { Residential, Shopping, Park, Riverside, Apartments }
[Serializable] public sealed class DistrictTemplate
{
    public DistrictKind kind;
    [Range(.1f,2f)] public float demand=1;
    [Range(.1f,1f)] public float traffic=.9f;
    public Vector3 pizzaWeights=new(3,1,1);
    public bool shortcut=true,trees,buildings,river;
    public Vector2[] roadPorts={new(0,0),new(1,0),new(1,-1),new(0,-1)};
    public Vector2[] addressPositions={new(.6f,-.3f),new(.6f,-.7f)};
    public string title="住宅街";
}
[Serializable] public sealed class CityGenerationSettings
{
    public int[] stageDays={1,6,11,21};
    [SerializeField] private int[] campaignStageBlocks={5,8,10,12};
    public int[] stageBlocks=>campaignStageBlocks;
    public int[] legacyStageBlocks={3,5,8,12};
    public int BlocksForVersion(int day,int version)=>version<=2?legacyStageBlocks[Mathf.Min(Stage(day),legacyStageBlocks.Length-1)]:Blocks(day);
    [Min(1)] public int retryCount=6;
    [Min(1)] public int columns=4;
    [Min(1)] public float blockSize=4,blockGap=1;
    [Min(.1f)] public float roadDistanceScale=1.5f;
    [Min(.1f)] public float shortcutDistance=9;
    [Min(0)] public float minimumShortcutSaving=.03f;
    [Range(0,1)] public float maximumShortcutSaving=.7f,maximumDeadEndFraction=.3f;
    [Min(0)] public float minimumNodeSpacing=.5f;
    [Min(1)] public int minimumEdges=6,maximumEdges=300,minimumDestinations=2,maximumDestinations=40;
    [Min(1)] public float maximumTotalDistance=4000;
    [Min(1)] public int constructionStartDay=11,constructionDays=3;
    [Range(.1f,1f)] public float constructionTraffic=.45f;
    [Min(.01f)] public float nearbyDistance=10;
    public DistrictTemplate[] templates={
        new(){kind=DistrictKind.Residential,title="住宅街",pizzaWeights=new(5,2,1)},
        new(){kind=DistrictKind.Shopping,buildings=true,title="商店街",demand=1.3f,pizzaWeights=new(2,2,5),traffic=.75f},
        new(){kind=DistrictKind.Park,trees=true,title="公園通り",demand=.6f,shortcut=false,pizzaWeights=new(5,1,1)},
        new(){kind=DistrictKind.Riverside,river=true,title="川沿い",demand=.9f,pizzaWeights=new(3,2,4)},
        new(){kind=DistrictKind.Apartments,buildings=true,title="団地",demand=1.5f,pizzaWeights=new(2,5,1)}};
    public int Stage(int day)
    {
        int stage=0;
        for(int i=0;i<stageDays.Length;i++)if(day>=stageDays[i])stage=i;
        return Mathf.Min(stage,stageBlocks.Length-1);
    }
    public int Blocks(int day)=>stageBlocks[Stage(day)];
}
public sealed class CityBlock
{
    public int index;public Rect area;public DistrictTemplate template;public string[] addresses;
}
public sealed class GeneratedCity
{
    public int seed,attempt,day;public bool fallback;public MapDefinition map;
    public readonly List<CityBlock> blocks=new();
    public readonly List<string> bridges=new(),construction=new();
    public CityMetrics metrics;
    public DistrictTemplate DistrictFor(string node)=>blocks.FirstOrDefault(b=>b.addresses.Contains(node))?.template;
}

/// <summary>Deterministic template placement. Prefixes and each block's RNG are independent of later expansion.</summary>
public static class CityGenerator
{
    public static GeneratedCity Build(int seed,int blockCount,CityGenerationSettings s,int variant=0)
    {
        if(blockCount<1 || s.templates==null || s.templates.Length<1)throw new ArgumentException("City needs blocks and templates.");
        var city=new GeneratedCity{seed=seed,map=ScriptableObject.CreateInstance<MapDefinition>()};city.map.hideFlags=HideFlags.DontSave;
        var m=city.map;
        m.Nodes.AddRange(new[]{new MapNode("Store",new(-4,0),NodeType.Intersection,true),new MapNode("N1",new(-1.5f,0),NodeType.Intersection,false),new MapNode("N2",new(1.5f,0),NodeType.Intersection,false),new MapNode("Goal1",new(4,-1),NodeType.DeliveryAddress,false),new MapNode("Goal2",new(4,-3),NodeType.DeliveryAddress,false)});
        m.Edges.AddRange(new[]{new MapEdge("Store-N1","Store","N1",10,RoadType.Main,.8f),new MapEdge("N1-N2-Main","N1","N2",10,RoadType.Main,.6f),new MapEdge("N1-N2-Backstreet","N1","N2",11,RoadType.Backstreet,1),new MapEdge("N2-Goal1","N2","Goal1",5,RoadType.Main,1),new MapEdge("N2-Goal2","N2","Goal2",7,RoadType.Main,1),new MapEdge("Goal1-Goal2","Goal1","Goal2",3,RoadType.Main,1)});
        for(int i=0;i<blockCount;i++)
        {
            var rng=new System.Random(unchecked(seed+i*104729+variant*7919));
            // Force a park and a river in the initial prefix so variety and bridge safety never depend on later blocks.
            var template=i==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:i==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[rng.Next(s.templates.Length)];
            int col=i%s.columns,row=i/s.columns;float size=s.blockSize,x=7+col*(size+s.blockGap),y=-row*(size+s.blockGap);
            string a=$"B{i}_A",b=$"B{i}_B",c=$"B{i}_C",d=$"B{i}_D",h1=$"B{i}_H1",h2=$"B{i}_H2";
            var area=new Rect(x,y-size,size,size);
            var block=new CityBlock{index=i,template=template,area=area,addresses=new[]{h1,h2}};
            city.blocks.Add(block);
            if(template.roadPorts.Length!=4 || template.addressPositions.Length!=2)throw new ArgumentException("Template needs four ports and two address candidates.");
            Vector2 At(Vector2 local)=>new Vector2(x,y)+local*size;
            m.Nodes.AddRange(new[]{new MapNode(a,At(template.roadPorts[0]),NodeType.Intersection,false),new MapNode(b,At(template.roadPorts[1]),NodeType.Intersection,false),new MapNode(c,At(template.roadPorts[2]),NodeType.Intersection,false),new MapNode(d,At(template.roadPorts[3]),NodeType.Intersection,false),new MapNode(h1,At(template.addressPositions[0]),NodeType.DeliveryAddress,false),new MapNode(h2,At(template.addressPositions[1]),NodeType.DeliveryAddress,false)});
            void Edge(string id,string from,string to,float distance,RoadType type=RoadType.Main,float traffic=1,bool both=true)=>m.Edges.Add(new MapEdge($"B{i}_"+id,from,to,distance,type,traffic,both));
            float length=size*s.roadDistanceScale;
            Edge("AB",a,b,length,traffic:template.traffic);Edge("BC",b,c,length,traffic:template.traffic);Edge("CD",c,d,length);Edge("DA",d,a,length);
            Edge("House1",b,h1,length*.4f);Edge("Pair",h1,h2,length*.4f);Edge("House2",h2,c,length*.4f);
            if(template.shortcut)Edge("Shortcut",a,c,s.shortcutDistance,RoadType.Backstreet);
            // A one-way local service road never replaces the bidirectional perimeter.
            if(i>=3 && rng.Next(2)==0)Edge("OneWay",d,h2,length*.7f,RoadType.Backstreet,1,false);
            if(col>0){Edge("JoinTop",$"B{i-1}_B",a,s.blockGap*s.roadDistanceScale);Edge("JoinBottom",$"B{i-1}_C",d,s.blockGap*s.roadDistanceScale);}
            else if(row>0)
            {
                Edge("JoinLeft",$"B{i-s.columns}_D",a,s.blockGap*s.roadDistanceScale);
                Edge("JoinRight",$"B{i-s.columns}_C",b,s.blockGap*s.roadDistanceScale);
            }
            else{Edge("Entry","N2",a,8);Edge("Loop","Store",d,18);}
            if(template.kind==DistrictKind.Riverside){city.bridges.Add($"B{i}_AB");city.bridges.Add($"B{i}_CD");}
        }
        return city;
    }
    public static GeneratedCity Generate(int seed,int blocks,CityGenerationSettings s)
    {
        for(int attempt=0;attempt<s.retryCount;attempt++)
        {
            GeneratedCity city=null;
            try
            {
                city=Build(seed,blocks,s,attempt);
                city.attempt=attempt;
                city.metrics=CityValidator.Measure(city,s);
                if(city.metrics.failures.Count==0)return city;
                Debug.LogWarning($"City rejected seed {seed} attempt {attempt}: "+string.Join("/",city.metrics.failures));
            }
            catch(Exception ex){Debug.LogWarning("City generation rejected: "+ex.Message);}
            if(city!=null)UnityEngine.Object.Destroy(city.map);
        }
        // Two explicitly selected, fixed layouts built from the authored safe loop template.
        var safe=SafeCity(seed,blocks,s);
        safe.fallback=true;
        safe.attempt=s.retryCount;
        Debug.LogWarning($"City fallback: seed {seed}, blocks {blocks}");
        return safe;
    }
    public static GeneratedCity SafeCity(int seed,int blocks,CityGenerationSettings settings)
    {
        var safe=Build(seed%2==0?1401:1402,blocks,new CityGenerationSettings{columns=settings.columns,blockSize=settings.blockSize,blockGap=settings.blockGap,roadDistanceScale=settings.roadDistanceScale,shortcutDistance=settings.shortcutDistance});
        safe.seed=seed;safe.metrics=CityValidator.Measure(safe,settings);return safe;
    }
}
