using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>v10: a planned new town (ニュータウン, like 美しが丘 near たまプラーザ). Superblocks are laid like bricks, so collector
/// roads meet in T-junctions rather than a strict grid; inside each superblock the residential streets follow one tidy
/// pattern (two parallel streets, or a loop) and bow slightly. One smooth warp of the whole town bends the long roads.
/// The graph stays straight edges with the authored distances; RoadGeometry draws the bends as curves.
/// Districts grow outwards one superblock at a time and never move earlier nodes or roads.</summary>
public static partial class CityGenerator
{
    private static GeneratedCity BuildNewTown(int seed,int blockCount,CityGenerationSettings s,int variant,int version,int savedHousesPerBlock)
    {
        int houses=s.HousesFor(version,savedHousesPerBlock);
        var city=new GeneratedCity{seed=seed,version=version,housesPerBlock=houses,map=ScriptableObject.CreateInstance<MapDefinition>()};
        var map=city.map;map.hideFlags=HideFlags.DontSave;
        float C=s.newTownCellSize,step=C/4;
        var random=new System.Random(unchecked(seed*486187739+variant*7919+15485863));
        float Next()=>(float)random.NextDouble();

        // ---- one smooth warp for the whole town (the store stays at the origin) ----
        float turn=Next()*Mathf.PI*2,p1=Next()*6.3f,p2=Next()*6.3f,p3=Next()*6.3f,p4=Next()*6.3f,wave=s.newTownWarpLength,bend=s.newTownWarp;
        Vector2 Raw(Vector2 p)
        {
            var q=new Vector2(p.x+bend*(Mathf.Sin(p.y/wave+p1)+.5f*Mathf.Sin(p.x/(wave*1.7f)+p3)),
                              p.y+bend*(Mathf.Sin(p.x/wave+p2)+.5f*Mathf.Sin(p.y/(wave*1.3f)+p4)));
            float c=Mathf.Cos(turn),sn=Mathf.Sin(turn);return new Vector2(q.x*c-q.y*sn,q.x*sn+q.y*c);
        }
        Vector2 origin=Raw(Vector2.zero);
        Vector2 Warp(Vector2 p)=>Raw(p)-origin;

        var positions=new Dictionary<string,Vector2>();
        var atLattice=new Dictionary<Vector2Int,string>();
        int sequence=0;
        Vector2Int Key(Vector2 lattice)=>new Vector2Int(Mathf.RoundToInt(lattice.x*8),Mathf.RoundToInt(lattice.y*8));
        string Node(string id,Vector2 lattice,NodeType type=NodeType.Intersection,Vector2? drawn=null)
        {
            if(atLattice.TryGetValue(Key(lattice),out var existing))return existing;
            var p=drawn??Warp(lattice);
            positions.Add(id,p);atLattice[Key(lattice)]=id;
            map.Nodes.Add(new MapNode(id,p,type,id=="Store"));
            return id;
        }
        MapEdge Road(string from,string to,RoadType type,string kind)
        {
            var edge=new MapEdge($"S{sequence++}_{kind}_{from}_{to}",from,to,Vector2.Distance(positions[from],positions[to])*s.roadDistanceScale,type,1,true);
            map.Edges.Add(edge);return edge;
        }
        string At(Vector2 lattice)=>atLattice.TryGetValue(Key(lattice),out var id)?id:throw new InvalidOperationException("Missing boundary point "+lattice);

        // ---- collector roads: the superblock edges, shared by neighbours and made once ----
        var made=new HashSet<string>();
        string Vertex(int k,int j)=>Node(k==0 && j==0?"Store":$"V{k}_{j}",new Vector2(k*C/2,j*C));
        void Segment(string key,int k0,int j0,int k1,int j1,int pieces)
        {
            if(!made.Add(key))return;
            string from=Vertex(k0,j0),to=Vertex(k1,j1);
            Vector2 a=new Vector2(k0*C/2,j0*C),b=new Vector2(k1*C/2,j1*C);
            string previous=from;
            for(int n=1;n<pieces;n++){string mid=Node($"{key}_{n}",Vector2.Lerp(a,b,(float)n/pieces));Road(previous,mid,RoadType.Main,"Street");previous=mid;}
            Road(previous,to,RoadType.Main,"Street");
        }
        int Parity(int j)=>((j%2)+2)%2;
        int K0(int i,int j)=>2*i+Parity(j);
        void Boundary(int i,int j)
        {
            int k0=K0(i,j);
            Segment($"H{k0}_{j}",k0,j,k0+1,j,2);Segment($"H{k0+1}_{j}",k0+1,j,k0+2,j,2);
            Segment($"H{k0}_{j+1}",k0,j+1,k0+1,j+1,2);Segment($"H{k0+1}_{j+1}",k0+1,j+1,k0+2,j+1,2);
            Segment($"W{k0}_{j}",k0,j,k0,j+1,4);Segment($"W{k0+2}_{j}",k0+2,j,k0+2,j+1,4);
        }

        // ---- the store and the first two homes, on the road through the store ----
        Segment("H0_0",0,0,1,0,2);Segment("H-1_0",-1,0,0,0,2);
        void FirstHome(string id,float x)
        {
            string mid=At(new Vector2(x*2,0));
            var host=map.Edges.First(e=>e.NodeAId=="Store" && e.NodeBId==mid || e.NodeBId=="Store" && e.NodeAId==mid);
            map.Edges.Remove(host);Node(id,new Vector2(x,0),NodeType.DeliveryAddress);
            Road("Store",id,RoadType.Main,"Street");Road(id,mid,RoadType.Main,"Street");
        }
        FirstHome("Goal1",step/2);FirstHome("Goal2",-step/2);

        // ---- superblocks grow outwards from the store, each next to the town so far ----
        Vector2 Center(Vector2Int c)=>new Vector2(K0(c.x,c.y)*C/2+C/2,c.y*C+C/2);
        IEnumerable<Vector2Int> Neighbours(Vector2Int c)
        {
            yield return new Vector2Int(c.x-1,c.y);yield return new Vector2Int(c.x+1,c.y);
            foreach(int dj in new[]{-1,1})
            {
                int j=c.y+dj,k0=K0(c.x,c.y);
                foreach(int k in new[]{k0-1,k0+1})if(((k-Parity(j))%2+2)%2==0)yield return new Vector2Int((k-Parity(j))/2,j);
            }
        }
        var noise=new Dictionary<Vector2Int,float>();
        float Order(Vector2Int c){if(!noise.ContainsKey(c))noise[c]=(float)new System.Random(unchecked(seed*31+c.x*7349+c.y*92821+variant)).NextDouble();return Center(c).magnitude+noise[c]*C*.6f;}
        var placed=new HashSet<Vector2Int>();
        var frontier=new HashSet<Vector2Int>{new(0,0),new(-1,0),new(-1,-1)};
        for(int district=0;district<blockCount;district++)
        {
            var cell=frontier.OrderBy(Order).ThenBy(c=>c.x).ThenBy(c=>c.y).First();
            frontier.Remove(cell);placed.Add(cell);
            foreach(var n in Neighbours(cell))if(!placed.Contains(n))frontier.Add(n);
            random=new System.Random(unchecked(seed*486187739+district*104729+variant*7919+15485863));
            var template=district==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:
                district==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[random.Next(s.templates.Length)];
            Boundary(cell.x,cell.y);
            Vector2 corner=new Vector2(K0(cell.x,cell.y)*C/2,cell.y*C);
            Vector2 L(float u,float v)=>corner+new Vector2(u,v)*(C/5);
            var inner=new List<MapEdge>();var districtPositions=new List<Vector2>();int junction=0;
            string Inner(float u,float v,Vector2 bow)
            {
                // Internal points are named per district; the bow moves only the drawn point, never the lattice key.
                var lattice=L(u,v);string id=$"B{district}_J{junction}";
                string got=Node(id,lattice,NodeType.Intersection,Warp(lattice+bow*(C/5)));
                if(got==id){junction++;districtPositions.Add(positions[id]);}
                return got;
            }
            string Edge(float u,float v)=>At(L(u,v));
            void Street(params string[] chain){for(int n=1;n<chain.Length;n++)inner.Add(Road(chain[n-1],chain[n],RoadType.Main,"Local"));}
            void Alley(string a,string b)=>inner.Add(Road(a,b,RoadType.Backstreet,"Local"));
            float bowA=(Next()<.5f?-1:1)*s.newTownBow*(.6f+Next()*.8f),bowB=(Next()<.5f?-1:1)*s.newTownBow*(.6f+Next()*.8f);
            float Arc(float t)=>Mathf.Sin(Mathf.PI*t/5);
            int pattern=random.Next(3);
            if(pattern==0)
            {
                // Two streets across, joined by one up the middle; an alley cuts the corner.
                string[] Row(float v,float b)=>new[]{Edge(0,v),Inner(1.25f,v,new Vector2(0,b*Arc(1.25f))),Inner(2.5f,v,new Vector2(0,b)),Inner(3.75f,v,new Vector2(0,b*Arc(3.75f))),Edge(5,v)};
                var low=Row(1.25f,bowA);var high=Row(3.75f,bowB);
                Street(low);Street(high);
                string middle=Inner(2.5f,2.5f,new Vector2(bowA*.5f,0));
                Street(Edge(2.5f,0),low[2],middle,high[2],Edge(2.5f,5));
                if(Next()<.5f)Alley(Edge(0,0),low[1]);else Alley(Edge(5,5),high[3]);
            }
            else if(pattern==1)
            {
                // Two streets up, joined by one across the middle.
                string[] Column(float u,float b)=>new[]{Edge(u,0),Inner(u,1.25f,new Vector2(b*Arc(1.25f),0)),Inner(u,2.5f,new Vector2(b,0)),Inner(u,3.75f,new Vector2(b*Arc(3.75f),0)),Edge(u,5)};
                var left=Column(1.25f,bowA);var right=Column(3.75f,bowB);
                Street(left);Street(right);
                string middle=Inner(2.5f,2.5f,new Vector2(0,bowA*.5f));
                Street(Edge(0,2.5f),left[2],middle,right[2],Edge(5,2.5f));
                if(Next()<.5f)Alley(Edge(5,5),right[3]);else Alley(Edge(0,0),left[1]);
            }
            else
            {
                // A loop street (an oval), reached from the four sides.
                float rx=1.5f+Next()*.2f,ry=1.5f+Next()*.2f;var ring=new string[8];
                for(int n=0;n<8;n++)
                {
                    float a=n*Mathf.PI/4;
                    var point=new Vector2(2.5f+Mathf.Cos(a)*rx,2.5f+Mathf.Sin(a)*ry);
                    ring[n]=Inner(point.x,point.y,Vector2.zero);
                }
                for(int n=0;n<8;n++)inner.Add(Road(ring[n],ring[(n+1)%8],RoadType.Main,"Local"));
                Street(Edge(5,2.5f),ring[0]);Street(Edge(2.5f,5),ring[2]);Street(Edge(0,2.5f),ring[4]);Street(Edge(2.5f,0),ring[6]);
                if(Next()<.5f)Alley(Edge(0,0),ring[5]);else Alley(Edge(5,5),ring[1]);
            }
            // Homes on this superblock's residential streets (the collector roads if those run out).
            var addresses=new string[houses];
            float spacing=Mathf.Max(s.minimumNodeSpacing*1.05f,step*.42f);
            var hosts=inner.Where(e=>e.RoadType==RoadType.Main).OrderBy(_=>Next()).ToList();
            hosts.AddRange(map.Edges.Where(e=>e.Id.Contains("_Street_") && districtPositions.Count>0 && Vector2.Distance((positions[e.NodeAId]+positions[e.NodeBId])/2,Warp(L(2.5f,2.5f)))<C*.62f).OrderBy(_=>Next()));
            int placedHomes=0;
            foreach(var host in hosts)
            {
                if(placedHomes>=houses)break;
                if(!map.Edges.Contains(host))continue;
                Vector2 a=positions[host.NodeAId],b=positions[host.NodeBId];
                var p=Vector2.Lerp(a,b,.45f+Next()*.1f);
                if(positions.Values.Any(q=>Vector2.Distance(q,p)<spacing))continue;
                string id=$"B{district}_H{placedHomes+1}";
                map.Edges.Remove(host);inner.Remove(host);
                positions.Add(id,p);map.Nodes.Add(new MapNode(id,p,NodeType.DeliveryAddress,false));
                string kind=host.Id.Contains("_Street_")?"Street":"Local";
                inner.Add(Road(host.NodeAId,id,host.RoadType,kind));inner.Add(Road(id,host.NodeBId,host.RoadType,kind));
                addresses[placedHomes++]=id;districtPositions.Add(p);
            }
            if(placedHomes<houses)throw new InvalidOperationException("Superblock has no room for its homes.");
            city.blocks.Add(new CityBlock{index=district,template=template,addresses=addresses,
                area=Rect.MinMaxRect(districtPositions.Min(p=>p.x),districtPositions.Min(p=>p.y),districtPositions.Max(p=>p.x),districtPositions.Max(p=>p.y))});
        }
        city.bridges.AddRange(map.Edges.Where(e=>e.RoadType==RoadType.Main && e.Id.Contains("_Street_") && e.NodeAId!="Store" && e.NodeBId!="Store").Take(2).Select(e=>e.Id));
        return city;
    }
}
