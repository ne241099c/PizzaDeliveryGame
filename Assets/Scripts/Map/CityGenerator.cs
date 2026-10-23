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
    public Vector2[] additionalAddressPositions={new(.25f,-.3f),new(.25f,-.7f)};
    public string title="住宅街";
}
[Serializable] public sealed class CityGenerationSettings
{
    public int[] stageDays={1,6,11,21};
    [SerializeField] private int[] campaignStageBlocks={5,8,10,12};
    [SerializeField, Tooltip("v7の開始・拡張地区数。旧世代の保存データには旧段階を使います。")] private int[] networkStageBlocks={8,10,12,14};
    public int[] stageBlocks=>networkStageBlocks;
    public int[] legacyStageBlocks={3,5,8,12};
    [Range(2,4), Tooltip("新規ゲームの街区ごとの家の数。既存セーブには保存時の数を使います。 ")] public int housesPerBlock=4;
    public int BlocksForVersion(int day,int version)=>version<=2?legacyStageBlocks[Mathf.Min(Stage(day),legacyStageBlocks.Length-1)]:version<7?campaignStageBlocks[Mathf.Min(Stage(day),campaignStageBlocks.Length-1)]:Blocks(day);
    [Min(1)] public int retryCount=6;
    [Min(1)] public int columns=4;
    [Min(1)] public float blockSize=4,blockGap=1;
    [Min(1), Tooltip("店舗から四方向の最初の街区までの距離。既存v1〜4の配置には影響しません。 ")] public float radialFirstBlockDistance=11;
    [Min(3), Tooltip("v6の街区の最小幅・高さ。既存v1〜5の配置には影響しません。 ")] public float organicMinimumBlockSize=5;
    [Min(3), Tooltip("v6の街区の最大幅・高さ。軸ごとに幅を変えて区画の反復を減らします。 ")] public float organicMaximumBlockSize=8;
    [Range(0,.8f), Tooltip("共有交差点のずれ。街区の幅とは別に交差点をずらします。 ")] public float organicJunctionJitter=.45f;
    [Range(.5f,1.5f), Tooltip("v6の裏道距離の倍率。道路の実際の長さに掛けます。 ")] public float organicShortcutDistanceFactor=1;
    [Min(6), Tooltip("v7の街路の広がり。点数に応じて徐々に外側まで街が育ちます。")] public float networkInitialRadius=7;
    [Min(.5f), Tooltip("v7の街が広がる速さ。街路点の数の平方根に掛けます。")] public float networkGrowthSpacing=1.8f;
    [Min(.8f), Tooltip("v7の街路点どうしの最低間隔。")] public float networkPointSpacing=2.1f;
    [Min(.1f), Tooltip("新しい道が既存の交差点の脇をすり抜けないための余白。")] public float networkRoadClearance=.65f;
    [Min(4), Tooltip("v7の近隣道路を結ぶ最大距離。")] public float networkMaximumConnection=11;
    [Range(0,1), Tooltip("接続可能な近隣道を見つけた時、迂回路を作る割合。")] public float networkLoopChance=.95f;
    [Range(0,1), Tooltip("迂回路のうち土地勘の必要な生活道路にする割合。")] public float networkShortcutChance=.65f;
    [Range(0,3), Tooltip("v7の一方通行の最大本数。0ならすべて双方向。")] public int networkMaximumOneWayRoads=0;
    [Min(.1f)] public float roadDistanceScale=1.5f;
    [Min(.1f)] public float shortcutDistance=9;
    [Min(0)] public float minimumShortcutSaving=.03f;
    [Range(0,1)] public float maximumShortcutSaving=.7f,maximumDeadEndFraction=.3f;
    [Min(0)] public float minimumNodeSpacing=.5f;
    [Min(1)] public int minimumEdges=6,maximumEdges=300,minimumDestinations=2,maximumDestinations=64;
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
    public int seed,attempt,day,version,housesPerBlock;public bool fallback;public MapDefinition map;
    public readonly List<CityBlock> blocks=new();
    public readonly List<string> bridges=new(),construction=new();
    public CityMetrics metrics;
    public DistrictTemplate DistrictFor(string node)=>blocks.FirstOrDefault(b=>b.addresses.Contains(node))?.template;
}

/// <summary>Deterministic template placement. Prefixes and each block's RNG are independent of later expansion.</summary>
public static class CityGenerator
{
    public const int CurrentVersion=7;
    public static GeneratedCity Build(int seed,int blockCount,CityGenerationSettings s,int variant=0,int version=CurrentVersion,int savedHousesPerBlock=0)
    {
        if(blockCount<1 || s.templates==null || s.templates.Length<1)throw new ArgumentException("City needs blocks and templates.");
        if(version>=7)return BuildStreetNetwork(seed,blockCount,s,variant,version,savedHousesPerBlock);
        if(version>=6)return BuildOrganic(seed,blockCount,s,variant,version,savedHousesPerBlock);
        int houseCount=version<4?2:Mathf.Clamp(savedHousesPerBlock>0?savedHousesPerBlock:s.housesPerBlock,2,4);
        var city=new GeneratedCity{seed=seed,version=version,housesPerBlock=houseCount,map=ScriptableObject.CreateInstance<MapDefinition>()};city.map.hideFlags=HideFlags.DontSave;
        var m=city.map;
        m.Nodes.AddRange(new[]{new MapNode("Store",new(-4,0),NodeType.Intersection,true),new MapNode("N1",new(-1.5f,0),NodeType.Intersection,false),new MapNode("N2",new(1.5f,0),NodeType.Intersection,false),new MapNode("Goal1",new(4,-1),NodeType.DeliveryAddress,false),new MapNode("Goal2",new(4,-3),NodeType.DeliveryAddress,false)});
        if(version>=5)
        {
            // Keep the small authored east approach, but make the shop the town's origin.
            for(int n=0;n<m.Nodes.Count;n++)
            {
                var node=m.Nodes[n];
                m.Nodes[n]=new MapNode(node.Id,node.Position+new Vector2(4,0),node.NodeType,node.IsStore);
            }
        }
        m.Edges.AddRange(new[]{new MapEdge("Store-N1","Store","N1",10,RoadType.Main,.8f),new MapEdge("N1-N2-Main","N1","N2",10,RoadType.Main,.6f),new MapEdge("N1-N2-Backstreet","N1","N2",11,RoadType.Backstreet,1),new MapEdge("N2-Goal1","N2","Goal1",5,RoadType.Main,1),new MapEdge("N2-Goal2","N2","Goal2",7,RoadType.Main,1),new MapEdge("Goal1-Goal2","Goal1","Goal2",3,RoadType.Main,1)});
        for(int i=0;i<blockCount;i++)
        {
            var rng=new System.Random(unchecked(seed+i*104729+variant*7919));
            // Force a park and a river in the initial prefix so variety and bridge safety never depend on later blocks.
            var template=i==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:i==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[rng.Next(s.templates.Length)];
            int col=i%s.columns,row=i/s.columns;float size=s.blockSize,x=7+col*(size+s.blockGap),y=-row*(size+s.blockGap);
            if(version>=5)
            {
                x=s.radialFirstBlockDistance+(i/4)*(size+s.blockGap);
                y=size*.5f;
            }
            string a=$"B{i}_A",b=$"B{i}_B",c=$"B{i}_C",d=$"B{i}_D",h1=$"B{i}_H1",h2=$"B{i}_H2";
            Vector2 Rotate(Vector2 point)
            {
                if(version<5)return point;
                return (i%4) switch {1=>new(-point.y,point.x),2=>-point,3=>new(point.y,-point.x),_=>point};
            }
            var corner1=Rotate(new Vector2(x,y-size));
            var corner2=Rotate(new Vector2(x+size,y));
            var area=Rect.MinMaxRect(Mathf.Min(corner1.x,corner2.x),Mathf.Min(corner1.y,corner2.y),Mathf.Max(corner1.x,corner2.x),Mathf.Max(corner1.y,corner2.y));
            var addresses=Enumerable.Range(1,houseCount).Select(h=>$"B{i}_H{h}").ToArray();
            var block=new CityBlock{index=i,template=template,area=area,addresses=addresses};
            city.blocks.Add(block);
            if(template.roadPorts.Length!=4 || template.addressPositions.Length!=2)throw new ArgumentException("Template needs four ports and two address candidates.");
            Vector2 At(Vector2 local)=>Rotate(new Vector2(x,y)+local*size);
            m.Nodes.AddRange(new[]{new MapNode(a,At(template.roadPorts[0]),NodeType.Intersection,false),new MapNode(b,At(template.roadPorts[1]),NodeType.Intersection,false),new MapNode(c,At(template.roadPorts[2]),NodeType.Intersection,false),new MapNode(d,At(template.roadPorts[3]),NodeType.Intersection,false),new MapNode(h1,At(template.addressPositions[0]),NodeType.DeliveryAddress,false),new MapNode(h2,At(template.addressPositions[1]),NodeType.DeliveryAddress,false)});
            void Edge(string id,string from,string to,float distance,RoadType type=RoadType.Main,float traffic=1,bool both=true)=>m.Edges.Add(new MapEdge($"B{i}_"+id,from,to,distance,type,traffic,both));
            float length=size*s.roadDistanceScale;
            Edge("AB",a,b,length,traffic:template.traffic);Edge("BC",b,c,length,traffic:template.traffic);Edge("CD",c,d,length);Edge("DA",d,a,length);
            Edge("House1",b,h1,length*.4f);Edge("Pair",h1,h2,length*.4f);Edge("House2",h2,c,length*.4f);
            // A second row of homes has its own connected local road. Keep legacy IDs,
            // positions, road costs and random draws unchanged for older saves.
            if(houseCount>2)
            {
                if(template.additionalAddressPositions==null || template.additionalAddressPositions.Length<houseCount-2)
                    throw new ArgumentException("Template needs additional address candidates for the configured density.");
                string h3=addresses[2];
                m.Nodes.Add(new MapNode(h3,At(template.additionalAddressPositions[0]),NodeType.DeliveryAddress,false));
                Edge("House3",a,h3,length*.4f);
                if(houseCount>3)
                {
                    string h4=addresses[3];
                    m.Nodes.Add(new MapNode(h4,At(template.additionalAddressPositions[1]),NodeType.DeliveryAddress,false));
                    Edge("PairWest",h3,h4,length*.4f);
                    Edge("House4",h4,d,length*.4f);
                }
                else Edge("House3Return",h3,d,length*.8f);
            }
            if(template.shortcut)Edge("Shortcut",a,c,s.shortcutDistance,RoadType.Backstreet);
            // A one-way local service road never replaces the bidirectional perimeter.
            if(i>=3 && rng.Next(2)==0)Edge("OneWay",d,h2,length*.7f,RoadType.Backstreet,1,false);
            if(version>=5)
            {
                if(i>=4)
                {
                    Edge("JoinTop",$"B{i-4}_B",a,s.blockGap*s.roadDistanceScale);
                    Edge("JoinBottom",$"B{i-4}_C",d,s.blockGap*s.roadDistanceScale);
                }
                else
                {
                    Edge("Entry",i==0?"N2":"Store",a,i==0?8:s.radialFirstBlockDistance*s.roadDistanceScale);
                    Edge("Loop","Store",d,s.radialFirstBlockDistance*s.roadDistanceScale);
                }
            }
            else if(col>0){Edge("JoinTop",$"B{i-1}_B",a,s.blockGap*s.roadDistanceScale);Edge("JoinBottom",$"B{i-1}_C",d,s.blockGap*s.roadDistanceScale);}
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
    // A street is a connection between independently sampled points, rather than
    // the perimeter of a repeated block. Append-only growth preserves every old
    // node and edge when the next district is unlocked.
    private static GeneratedCity BuildStreetNetwork(int seed,int blockCount,CityGenerationSettings s,int variant,int version,int savedHousesPerBlock)
    {
        int houses=Mathf.Clamp(savedHousesPerBlock>0?savedHousesPerBlock:s.housesPerBlock,2,4);
        var city=new GeneratedCity{seed=seed,version=version,housesPerBlock=houses,map=ScriptableObject.CreateInstance<MapDefinition>()};
        var map=city.map;map.hideFlags=HideFlags.DontSave;
        var positions=new Dictionary<string,Vector2>();
        var degree=new Dictionary<string,int>();
        var edges=new Dictionary<string,MapEdge>();
        var random=new System.Random(unchecked(seed*486187739+variant*7919+15485863));
        int sequence=0,shortcutCount=0,oneWayCount=0;
        float Next()=> (float)random.NextDouble();
        void Node(string id,Vector2 p,NodeType type=NodeType.Intersection)
        {
            positions.Add(id,p);degree.Add(id,0);
            map.Nodes.Add(new MapNode(id,p,type,id=="Store"));
        }
        string Road(string from,string to,RoadType type=RoadType.Main,bool both=true,bool local=false)
        {
            string id=$"S{sequence++}_{(local?"Local":"Street")}_{from}_{to}";
            var edge=new MapEdge(id,from,to,Vector2.Distance(positions[from],positions[to])*s.roadDistanceScale,type,1,both);
            map.Edges.Add(edge);edges.Add(id,edge);degree[from]++;degree[to]++;
            return id;
        }
        float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        float ToSegment(Vector2 p,Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a;
            float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(p,a+delta*t);
        }
        bool Crosses(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            float abC=Cross(b-a,c-a),abD=Cross(b-a,d-a);
            float cdA=Cross(d-c,a-c),cdB=Cross(d-c,b-c);
            return abC*abD<-.00001f && cdA*cdB<-.00001f;
        }
        bool Visible(Vector2 p,string target,string source=null)
        {
            Vector2 q=positions[target];
            foreach(var node in positions)
                if(node.Key!=source && node.Key!=target && ToSegment(node.Value,p,q)<s.networkRoadClearance)return false;
            foreach(var edge in map.Edges)
            {
                bool shares=edge.NodeAId==target || edge.NodeBId==target || source!=null && (edge.NodeAId==source || edge.NodeBId==source);
                if(!shares && Crosses(p,q,positions[edge.NodeAId],positions[edge.NodeBId]))return false;
                if(shares)
                {
                    string at=edge.NodeAId==target || edge.NodeBId==target?target:source;
                    string other=edge.NodeAId==at?edge.NodeBId:edge.NodeAId;
                    Vector2 direction=at==target?p-q:q-p;
                    if(Vector2.Angle(direction,positions[other]-positions[at])<27)return false;
                }
            }
            return true;
        }
        float MainDistance(string from,string to)
        {
            var route=RoadPathfinder.FindShortestPath(map.Edges.Where(e=>e.RoadType==RoadType.Main).ToArray(),from,to,e=>e.Distance);
            return route==null?float.PositiveInfinity:route.Sum(id=>edges[id].Distance);
        }
        Node("Store",Vector2.zero);
        // Two close addresses anchor short trips. Their unrelated bearings do not
        // determine the directions or outlines of the rest of the city.
        float angle=Next()*Mathf.PI*2;
        Vector2 Direction(float a)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a));
        Node("Goal1",Direction(angle)*(2.5f+Next()),NodeType.DeliveryAddress);
        Node("Goal2",Direction(angle+2.3f+Next()*.9f)*(3.2f+Next()),NodeType.DeliveryAddress);
        Road("Store","Goal1");Road("Store","Goal2");
        for(int district=0;district<blockCount;district++)
        {
            // Independent per-district random state also makes prefixes stable
            // when a later district needs more placement attempts.
            random=new System.Random(unchecked(seed*486187739+district*104729+variant*7919+15485863));
            var template=district==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:
                district==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[random.Next(s.templates.Length)];
            string[] addresses=Enumerable.Range(1,houses).Select(h=>$"B{district}_H{h}").ToArray();
            var districtPositions=new List<Vector2>();
            // A changing number of intersections avoids an identical street
            // motif per district. Houses themselves may lie on through streets.
            int junctions=2+random.Next(3),count=houses+junctions;
            for(int n=0;n<count;n++)
            {
                bool address=n<houses;
                string id=address?addresses[n]:$"B{district}_J{n-houses}";
                Vector2 point=Vector2.zero;string nearest=null;
                float radius=s.networkInitialRadius+Mathf.Sqrt(positions.Count)*s.networkGrowthSpacing;
                for(int attempt=0;attempt<800 && nearest==null;attempt++)
                {
                    point=Direction(Next()*Mathf.PI*2)*Mathf.Sqrt(Next())*radius;
                    if(positions.Values.Any(p=>Vector2.Distance(p,point)<Mathf.Max(s.minimumNodeSpacing,s.networkPointSpacing)))continue;
                    if(map.Edges.Any(e=>ToSegment(point,positions[e.NodeAId],positions[e.NodeBId])<s.networkRoadClearance))continue;
                    nearest=positions.Keys.Where(k=>degree[k]<4 && Vector2.Distance(point,positions[k])<s.networkMaximumConnection)
                        .OrderBy(k=>Vector2.SqrMagnitude(point-positions[k])).FirstOrDefault(k=>Visible(point,k));
                }
                if(nearest==null)throw new InvalidOperationException("Street point could not join the existing network.");
                Node(id,point,address?NodeType.DeliveryAddress:NodeType.Intersection);
                districtPositions.Add(point);Road(nearest,id);
                // Close some loops through another visible neighbour. Reject
                // triangles and almost-parallel junctions, producing irregular
                // lanes, T junctions and a few genuine dead ends.
                if(Next()>s.networkLoopChance)continue;
                var candidates=positions.Keys.Where(k=>k!=id && k!=nearest && degree[k]<4 &&
                        Vector2.Distance(point,positions[k])<s.networkMaximumConnection &&
                        !map.Edges.Any(e=>(e.NodeAId==nearest && e.NodeBId==k)||(e.NodeBId==nearest && e.NodeAId==k)))
                    .OrderBy(k=>Vector2.SqrMagnitude(point-positions[k])).ToArray();
                foreach(string other in candidates)
                {
                    if(!Visible(point,other,id))continue;
                    float direct=Vector2.Distance(point,positions[other])*s.roadDistanceScale;
                    float detour=MainDistance(id,other);
                    bool shortcut=detour>direct*1.1f && detour<direct*5f && (shortcutCount==0 || Next()<s.networkShortcutChance);
                    bool oneWay=shortcut && oneWayCount<s.networkMaximumOneWayRoads;
                    Road(id,other,shortcut?RoadType.Backstreet:RoadType.Main,!oneWay,true);
                    if(shortcut){shortcutCount++;if(oneWay)oneWayCount++;}
                    break;
                }
            }
            city.blocks.Add(new CityBlock{index=district,template=template,addresses=addresses,
                area=Rect.MinMaxRect(districtPositions.Min(p=>p.x),districtPositions.Min(p=>p.y),districtPositions.Max(p=>p.x),districtPositions.Max(p=>p.y))});
        }
        // These are actual connected street segments. Renderer may show the
        // short water crossing independently from district demand metadata.
        city.bridges.AddRange(map.Edges.Where(e=>e.RoadType==RoadType.Main && e.NodeAId!="Store" && e.NodeBId!="Store").Take(2).Select(e=>e.Id));
        return city;
    }
    // Each junction and each street has its own seed. Adding a block never moves
    // an existing junction or changes a shared street's direction, cost or traffic.
    private static GeneratedCity BuildOrganic(int seed,int blockCount,CityGenerationSettings s,int variant,int version,int savedHousesPerBlock)
    {
        int houses=Mathf.Clamp(savedHousesPerBlock>0?savedHousesPerBlock:s.housesPerBlock,2,4);
        var city=new GeneratedCity{seed=seed,version=version,housesPerBlock=houses,map=ScriptableObject.CreateInstance<MapDefinition>()};
        var m=city.map;m.hideFlags=HideFlags.DontSave;
        var positions=new Dictionary<string,Vector2>();
        var sharedRoads=new Dictionary<string,string>();
        System.Random RandomFor(int x,int y,int salt)=>new(unchecked(seed*486187739+x*104729+y*130363+variant*7919+salt*15485863));
        float Axis(int n,int salt)
        {
            float position=0;
            int sign=n<0?-1:1;
            for(int k=0;k<Mathf.Abs(n);k++)
            {
                int segment=n<0?-k-1:k;
                float low=Mathf.Max(3,s.organicMinimumBlockSize),high=Mathf.Max(low,s.organicMaximumBlockSize);
                position+=sign*Mathf.Lerp(low,high,(float)RandomFor(segment,0,salt).NextDouble());
            }
            return position;
        }
        Vector2 JunctionPosition(int x,int y)
        {
            if(x==0 && y==0)return Vector2.zero;
            var r=RandomFor(x,y,13);
            return new Vector2(Axis(x,1),Axis(y,2))+new Vector2((float)r.NextDouble()*2-1,(float)r.NextDouble()*2-1)*s.organicJunctionJitter;
        }
        string Junction(int x,int y)
        {
            string id=x==0 && y==0?"Store":$"J{x}_{y}";
            if(!positions.ContainsKey(id))
            {
                Vector2 p=JunctionPosition(x,y);positions.Add(id,p);
                m.Nodes.Add(new MapNode(id,p,NodeType.Intersection,id=="Store"));
            }
            return id;
        }
        void Node(string id,Vector2 p)
        {
            positions.Add(id,p);m.Nodes.Add(new MapNode(id,p,NodeType.DeliveryAddress,false));
        }
        string Road(string id,string from,string to,RoadType type=RoadType.Main,float traffic=1,bool both=true,float factor=1)
        {
            float distance=Vector2.Distance(positions[from],positions[to])*s.roadDistanceScale*factor;
            m.Edges.Add(new MapEdge(id,from,to,distance,type,traffic,both));return id;
        }
        string Street(string id,string from,string to)
        {
            string key=string.CompareOrdinal(from,to)<0?from+"|"+to:to+"|"+from;
            if(sharedRoads.TryGetValue(key,out string existing))return existing;
            // Congestion is tied to the street, not whichever district introduced it.
            float traffic=.75f+(float)RandomFor(StableHash(key),0,31).NextDouble()*.25f;
            if(from=="Store" && to=="J1_0" || to=="Store" && from=="J1_0")
            {
                var east=positions["J1_0"];
                if(!positions.ContainsKey("Goal1")){Node("Goal1",east*.3f);Node("Goal2",east*.65f);}
                Road("Store-Goal1","Store","Goal1",traffic:traffic);
                Road("Goal1-Goal2","Goal1","Goal2",traffic:traffic);
                Road("Goal2-East","Goal2","J1_0",traffic:traffic);
                id="Store-Goal1";
            }
            else Road(id,from,to,traffic:traffic);
            sharedRoads.Add(key,id);return id;
        }
        // Compact connected growth around the shop. Unlike four detached spokes,
        // neighbouring districts share their boundary roads and intersections.
        var cells=new[]{new Vector2Int(0,0),new(-1,0),new(-1,-1),new(0,-1),new(1,0),new(1,-1),new(0,1),new(-1,1),new(-2,0),new(-2,-1),new(0,-2),new(-1,-2)};
        Junction(0,0);
        for(int i=0;i<blockCount;i++)
        {
            Vector2Int cell=i<cells.Length?cells[i]:new Vector2Int(2+(i-cells.Length)/4,(i-cells.Length)%4-2);
            int x=cell.x,y=cell.y;
            string a=Junction(x,y+1),b=Junction(x+1,y+1),c=Junction(x+1,y),d=Junction(x,y);
            var rng=RandomFor(i,0,23);
            var template=i==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:i==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[rng.Next(s.templates.Length)];
            string ab=Street($"B{i}_AB",a,b);Street($"B{i}_BC",b,c);
            string cd=Street($"B{i}_CD",c,d);Street($"B{i}_DA",d,a);
            var ports=new[]{a,b,c,d};int orientation=rng.Next(4);
            a=ports[orientation];b=ports[(orientation+1)%4];c=ports[(orientation+2)%4];d=ports[(orientation+3)%4];
            Vector2 At(float u,float v)=>Vector2.Lerp(Vector2.Lerp(positions[d],positions[c],u),Vector2.Lerp(positions[a],positions[b],u),v);
            // Two local streets bend inward from the perimeter. Their different
            // setbacks produce small junctions and variable frontage lengths.
            float west=.24f+(float)rng.NextDouble()*.07f,east=.69f+(float)rng.NextDouble()*.07f;
            float upper=.67f+(float)rng.NextDouble()*.07f,lower=.26f+(float)rng.NextDouble()*.07f;
            string[] addresses=Enumerable.Range(1,houses).Select(h=>$"B{i}_H{h}").ToArray();
            Node(addresses[0],At(east,upper));Node(addresses[1],At(east,lower));
            if(houses==4)
            {
                Node(addresses[2],At(west,upper));Node(addresses[3],At(west,lower));
                int layout=rng.Next(3),sequence=0;
                void Chain(params string[] route)
                {for(int j=1;j<route.Length;j++)Road($"B{i}_Local{sequence++}",route[j-1],route[j]);}
                if(layout==0){Chain(a,addresses[2],addresses[3],d);Chain(b,addresses[0],addresses[1],c);}
                else if(layout==1)Chain(a,addresses[2],addresses[3],addresses[1],addresses[0],b);
                else{Chain(a,addresses[2],addresses[3],addresses[1],c);Chain(addresses[3],addresses[0]);}
                if(template.shortcut)Road($"B{i}_Shortcut",addresses[2],addresses[0],RoadType.Backstreet,factor:s.organicShortcutDistanceFactor);
                if(i>=2 && layout==0 && rng.Next(2)==0)
                    Road($"B{i}_OneWay",addresses[3],addresses[1],RoadType.Backstreet,both:false,factor:s.organicShortcutDistanceFactor);
            }
            else
            {
                if(houses==2)
                {
                    Road($"B{i}_House1",a,addresses[0]);Road($"B{i}_House1Return",addresses[0],b);
                    Road($"B{i}_House2Entry",d,addresses[1]);Road($"B{i}_House2",addresses[1],c);
                }
                else
                {
                    Road($"B{i}_House1",b,addresses[0]);Road($"B{i}_Pair",addresses[0],addresses[1]);Road($"B{i}_House2",addresses[1],c);
                    Node(addresses[2],At(west,upper));Road($"B{i}_House3",a,addresses[2]);Road($"B{i}_House3Return",addresses[2],d);
                }
                if(template.shortcut)Road($"B{i}_Shortcut",houses>2?addresses[2]:addresses[1],addresses[0],RoadType.Backstreet,factor:s.organicShortcutDistanceFactor);
            }
            var corners=new[]{positions[a],positions[b],positions[c],positions[d]};
            city.blocks.Add(new CityBlock{index=i,template=template,addresses=addresses,area=Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y))});
            if(template.kind==DistrictKind.Riverside){city.bridges.Add(ab);city.bridges.Add(cd);}
        }
        return city;
    }
    private static int StableHash(string value)
    {
        unchecked {int hash=17;foreach(char c in value)hash=hash*31+c;return hash;}
    }
    public static GeneratedCity Generate(int seed,int blocks,CityGenerationSettings s,int version=CurrentVersion,int savedHousesPerBlock=0)
    {
        if(version>=7)
        {
            // Pick one variant for the entire campaign, including future stages.
            // Retrying only the requested size could move the old town when an
            // expansion happens to need another variant.
            int[] sizes=s.stageBlocks.Concat(new[]{blocks}).Distinct().OrderBy(n=>n).ToArray();
            for(int attempt=0;attempt<s.retryCount;attempt++)
            {
                GeneratedCity requested=null;bool accepted=true;
                foreach(int size in sizes)
                {
                    GeneratedCity candidate=null;
                    try
                    {
                        candidate=Build(seed,size,s,attempt,version,savedHousesPerBlock);
                        candidate.attempt=attempt;candidate.metrics=CityValidator.Measure(candidate,s);
                        if(candidate.metrics.failures.Count>0)accepted=false;
                        if(size==blocks && accepted){requested=candidate;candidate=null;}
                    }
                    catch(Exception ex){accepted=false;Debug.LogWarning("Street candidate rejected: "+ex.Message);}
                    if(candidate!=null)UnityEngine.Object.Destroy(candidate.map);
                    if(!accepted)break;
                }
                if(accepted && requested!=null)return requested;
                if(requested!=null)UnityEngine.Object.Destroy(requested.map);
            }
            var fallback=SafeCity(seed,blocks,s,version,savedHousesPerBlock);
            fallback.fallback=true;fallback.attempt=s.retryCount;
            Debug.LogWarning($"City fallback: seed {seed}, blocks {blocks}");
            return fallback;
        }
        for(int attempt=0;attempt<s.retryCount;attempt++)
        {
            GeneratedCity city=null;
            try
            {
                city=Build(seed,blocks,s,attempt,version,savedHousesPerBlock);
                city.attempt=attempt;
                city.metrics=CityValidator.Measure(city,s);
                if(city.metrics.failures.Count==0)return city;
                Debug.LogWarning($"City rejected seed {seed} attempt {attempt}: "+string.Join("/",city.metrics.failures));
            }
            catch(Exception ex){Debug.LogWarning("City generation rejected: "+ex.Message);}
            if(city!=null)UnityEngine.Object.Destroy(city.map);
        }
        // Two explicitly selected, fixed layouts built from the authored safe loop template.
        var safe=SafeCity(seed,blocks,s,version,savedHousesPerBlock);
        safe.fallback=true;
        safe.attempt=s.retryCount;
        Debug.LogWarning($"City fallback: seed {seed}, blocks {blocks}");
        return safe;
    }
    public static GeneratedCity SafeCity(int seed,int blocks,CityGenerationSettings settings,int version=CurrentVersion,int savedHousesPerBlock=0)
    {
        var safe=Build(seed%2==0?1401:1402,blocks,new CityGenerationSettings{networkInitialRadius=settings.networkInitialRadius,networkGrowthSpacing=settings.networkGrowthSpacing,networkPointSpacing=settings.networkPointSpacing,networkRoadClearance=settings.networkRoadClearance,networkMaximumConnection=settings.networkMaximumConnection,networkLoopChance=settings.networkLoopChance,networkShortcutChance=settings.networkShortcutChance,networkMaximumOneWayRoads=settings.networkMaximumOneWayRoads,columns=settings.columns,organicMinimumBlockSize=settings.organicMinimumBlockSize,organicMaximumBlockSize=settings.organicMaximumBlockSize,organicJunctionJitter=settings.organicJunctionJitter,organicShortcutDistanceFactor=settings.organicShortcutDistanceFactor,radialFirstBlockDistance=settings.radialFirstBlockDistance,blockSize=settings.blockSize,blockGap=settings.blockGap,roadDistanceScale=settings.roadDistanceScale,shortcutDistance=settings.shortcutDistance,housesPerBlock=settings.housesPerBlock},version:version,savedHousesPerBlock:savedHousesPerBlock);
        safe.seed=seed;safe.metrics=CityValidator.Measure(safe,settings);return safe;
    }
}
