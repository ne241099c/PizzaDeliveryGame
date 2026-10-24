using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>v11: a suburb grown like 美しが丘 (たまプラーザ): a few long, curving avenues cross the town, and each neighbourhood
/// between them has its own street pattern and direction — tidy rows, loops, a spine with cul-de-sacs and hairpins, or
/// wandering lanes — joined to its neighbours wherever the streets meet. The whole town is built once from the seed;
/// districts reveal the neighbourhoods nearest the shop first, so opening a district only adds roads and homes.
/// Roads are straight edges with authored distances; RoadGeometry draws the bends.</summary>
public static partial class CityGenerator
{
    private sealed class PatchRoad {public string a,b,kind;public RoadType type;}
    private sealed class PatchGraph
    {
        public readonly Dictionary<string,Vector2> at=new();
        public readonly HashSet<string> homes=new(),keep=new();
        public readonly List<PatchRoad> roads=new();
        public readonly Dictionary<string,List<PatchRoad>> roadsAt=new();
        public float snap,edgeSnap;int count;
        public string NewNode(Vector2 p,string prefix="N"){string id=prefix+(count++);at[id]=p;roadsAt[id]=new List<PatchRoad>();return id;}
        public void Add(string a,string b,string kind,RoadType type)
        {
            if(a==b || Vector2.Distance(at[a],at[b])<.3f || roadsAt[a].Any(r=>r.a==b || r.b==b))return;
            var road=new PatchRoad{a=a,b=b,kind=kind,type=type};roads.Add(road);roadsAt[a].Add(road);roadsAt[b].Add(road);
        }
        public void Remove(PatchRoad r){roads.Remove(r);roadsAt[r.a].Remove(r);roadsAt[r.b].Remove(r);}
        public string Nearest(Vector2 p,float radius,string except=null)
        {
            string best=null;float distance=radius;
            foreach(var n in at)if(n.Key!=except && !homes.Contains(n.Key)){float d=Vector2.Distance(n.Value,p);if(d<distance){distance=d;best=n.Key;}}
            return best;
        }
        public bool Clear(Vector2 p,float radius)=>at.Values.All(q=>Vector2.Distance(p,q)>=radius);
        public static float ToSegment(Vector2 p,Vector2 a,Vector2 b,out float t)
        {
            var d=b-a;t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;return Vector2.Distance(p,a+d*t);
        }
        public string Split(PatchRoad r,Vector2 p)
        {
            Remove(r);string id=NewNode(p);Add(r.a,id,r.kind,r.type);Add(id,r.b,r.kind,r.type);return id;
        }
        /// <summary>An existing node near p, a new node on a road passing close by, or a new free node.</summary>
        public string Resolve(Vector2 p)
        {
            var near=Nearest(p,snap);if(near!=null)return near;
            foreach(var r in roads.ToArray())
            {
                if(ToSegment(p,at[r.a],at[r.b],out float t)>=edgeSnap)continue;
                var q=Vector2.Lerp(at[r.a],at[r.b],t);
                var close=Nearest(q,snap);if(close!=null)return close;
                return Split(r,q);
            }
            return NewNode(p);
        }
        static bool Cross(Vector2 a,Vector2 b,Vector2 c,Vector2 d,out float t)
        {
            t=0;Vector2 r=b-a,s=d-c;float den=r.x*s.y-r.y*s.x;if(Mathf.Abs(den)<1e-6f)return false;
            Vector2 ca=c-a;t=(ca.x*s.y-ca.y*s.x)/den;float u=(ca.x*r.y-ca.y*r.x)/den;
            return t>1e-4f && t<1-1e-4f && u>1e-4f && u<1-1e-4f;
        }
        /// <summary>Lays a straight road from a node towards q, joining every road it crosses. With <paramref name="stopAtAvenue"/>
        /// it ends at the first avenue (a T-junction). Returns the node it ended on.</summary>
        public string Lay(string from,Vector2 q,string kind,RoadType type,bool stopAtAvenue,out bool stopped)
        {
            stopped=false;string end=Resolve(q);var ignore=new HashSet<PatchRoad>();
            for(int guard=0;guard<40 && from!=end;guard++)
            {
                Vector2 p=at[from],target=at[end];PatchRoad hit=null;float best=2;
                foreach(var r in roads)
                {
                    if(ignore.Contains(r) || r.a==from || r.b==from || r.a==end || r.b==end)continue;
                    if(Cross(p,target,at[r.a],at[r.b],out float t) && t<best){best=t;hit=r;}
                }
                if(hit==null){Add(from,end,kind,type);return end;}
                var point=Vector2.Lerp(p,target,best);
                string junction=Nearest(point,snap);
                if(junction==from || junction==end){ignore.Add(hit);continue;}
                if(junction==null)
                {
                    PatchGraph.ToSegment(point,at[hit.a],at[hit.b],out float th);
                    junction=Split(hit,Vector2.Lerp(at[hit.a],at[hit.b],th));
                }
                Add(from,junction,kind,type);
                if(stopAtAvenue && hit.kind=="Avenue"){stopped=true;return junction;}
                from=junction;
            }
            return from;
        }
        public string Polyline(IList<Vector2> points,string kind,RoadType type,bool stopAtAvenue,string start=null)
        {
            string from=start??Resolve(points[0]);
            for(int i=1;i<points.Count;i++){from=Lay(from,points[i],kind,type,stopAtAvenue,out bool stopped);if(stopped)break;}
            return from;
        }
    }

    private static GeneratedCity BuildPatchwork(int seed,int blockCount,CityGenerationSettings s,int variant,int version,int savedHousesPerBlock)
    {
        int houses=s.HousesFor(version,savedHousesPerBlock);
        var city=new GeneratedCity{seed=seed,version=version,housesPerBlock=houses,map=ScriptableObject.CreateInstance<MapDefinition>()};
        var map=city.map;map.hideFlags=HideFlags.DontSave;
        var rng=new System.Random(unchecked(seed*486187739+variant*7919+15485863));
        float Next()=>(float)rng.NextDouble();
        float Range(float a,float b)=>a+(b-a)*Next();
        float step=s.patchStreetSpacing,R=s.patchTownRadius;
        var g=new PatchGraph{snap=s.minimumNodeSpacing*1.1f,edgeSnap=.3f};
        g.at["Store"]=Vector2.zero;g.roadsAt["Store"]=new List<PatchRoad>();

        // ---- avenues: long gentle curves across the town; the first passes the shop ----
        float baseAngle=Next()*Mathf.PI*2;
        int avenueCount=s.patchAvenues;
        for(int a=0;a<avenueCount;a++)
        {
            float angle=baseAngle+a*Mathf.PI/avenueCount+Range(-.25f,.25f);
            var dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var normal=new Vector2(-dir.y,dir.x);
            Vector2 o=a==0?Vector2.zero:normal*Range(3f,6.5f)*(Next()<.5f?-1:1);
            float amp=Range(.8f,1.8f),wave=Range(5f,8f),phase=a==0?0:Next()*6.3f;
            var points=new List<Vector2>();
            int span=Mathf.CeilToInt(R*1.3f/step);
            for(int k=-span;k<=span;k++)
            {
                float t=k*step;
                var p=k==0 && a==0?Vector2.zero:o+dir*t+normal*(amp*(Mathf.Sin(t/wave+phase)-Mathf.Sin(phase)));
                if(p.magnitude<=R+step)points.Add(p);
            }
            // The shop sits on the first avenue.
            if(a==0)
            {
                int i=points.FindIndex(p=>p.magnitude<.01f);
                if(i<0)throw new InvalidOperationException("The first avenue misses the shop.");
                g.Polyline(points.Take(i+1).Reverse().ToList(),"Avenue",RoadType.Main,false,"Store");
                g.Polyline(points.Skip(i).ToList(),"Avenue",RoadType.Main,false,"Store");
            }
            else if(points.Count>1)g.Polyline(points,"Avenue",RoadType.Main,false);
        }

        // ---- neighbourhoods: centres spread over the town, each with its own pattern and direction ----
        var centres=new List<Vector2>();
        for(int attempt=0;attempt<3000 && centres.Count<s.patchCount;attempt++)
        {
            var p=new Vector2(Range(-R,R),Range(-R,R));
            if(p.magnitude>R-1 || centres.Any(c=>Vector2.Distance(c,p)<s.patchSpacing) || p.magnitude<s.patchSpacing*.45f)continue;
            centres.Add(p);
        }
        // Reveal order: nearest the shop first, each next to one already open.
        var order=new List<int>();var left=Enumerable.Range(0,centres.Count).ToList();
        while(left.Count>0)
        {
            var candidates=order.Count==0?left:left.Where(i=>order.Any(o=>Vector2.Distance(centres[o],centres[i])<s.patchSpacing*1.75f)).ToList();
            if(candidates.Count==0)candidates=left;
            int next=candidates.OrderBy(i=>centres[i].magnitude).First();order.Add(next);left.Remove(next);
        }
        var centre=order.Select(i=>centres[i]).ToList();
        for(int d=0;d<centre.Count;d++)
        {
            var local=new System.Random(unchecked(seed*486187739+d*104729+variant*7919+15485863));
            float L()=>(float)local.NextDouble();float LR(float a,float b)=>a+(b-a)*L();
            float theta=L()*Mathf.PI;var U=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta));var V=new Vector2(-U.y,U.x);
            Vector2 c=centre[d];float ha=LR(2.6f,3.3f),hb=LR(2.5f,3.1f);
            Vector2 P(float u,float v)=>c+U*u+V*v;
            List<Vector2> Line(Vector2 from,Vector2 to,float bow)
            {
                var list=new List<Vector2>();int n=Mathf.Max(1,Mathf.RoundToInt(Vector2.Distance(from,to)/step));var nrm=new Vector2(-(to-from).y,(to-from).x).normalized;
                for(int i=0;i<=n;i++){float t=(float)i/n;list.Add(Vector2.Lerp(from,to,t)+nrm*bow*Mathf.Sin(t*Mathf.PI));}
                return list;
            }
            // A street from inside the neighbourhood outwards, ending at an avenue or reaching a little past the edge.
            void Out(Vector2 inside,Vector2 outside,string kind,float bow=0)=>g.Polyline(Line(inside,outside,bow),kind,RoadType.Main,true);
            int pattern=d==0?0:local.Next(4);
            // Every neighbourhood has its fabric of rows in its own direction, with short cross streets set like bricks;
            // a loop or a spine with cul-de-sacs takes one half, and wandering lanes cut across the rest.
            float spacing=LR(1.3f,1.75f),reach=LR(.6f,1.6f);var rows=new List<float>();
            float rowFrom=pattern==1 || pattern==2?-hb:-hb,rowTo=pattern==1 || pattern==2?-.35f:hb;
            for(float v=rowFrom;v<=rowTo+.01f;v+=spacing)rows.Add(v);
            foreach(float v in rows){float bow=LR(-.3f,.3f);string kind=rows.IndexOf(v)==rows.Count/2?"Street":"Local";Out(P(0,v),P(-ha-reach,v),kind,bow);Out(P(0,v),P(ha+reach,v),kind,-bow);}
            for(int r=0;r+1<rows.Count;r++)
            {
                float shift=r%2==0?0:LR(.9f,1.4f);
                for(float u=-ha+LR(.4f,1.2f)+shift;u<ha;u+=LR(2.2f,3f))g.Polyline(new[]{P(u,rows[r]),P(u+LR(-.3f,.3f),rows[r+1])},"Local",RoadType.Main,true);
            }
            if(rows.Count>0 && pattern!=0)
            {
                // The edge streets of the fabric reach out to the neighbours.
                for(float u=-ha+LR(.5f,1.5f);u<ha;u+=LR(2.5f,3.5f))Out(P(u,rows[0]),P(u,rows[0]-LR(1f,2f)),"Local");
            }
            if(pattern==1)
            {
                // One or two oval loops in the upper half, reached by spokes.
                int loops=L()<.5f?1:2;
                for(int k=0;k<loops;k++)
                {
                    var oc=P(loops==1?0:(k==0?-ha*.5f:ha*.5f),hb*.45f+LR(-.2f,.2f));float rx=LR(1.1f,1.5f),ry=LR(.9f,1.3f);var ring=new List<Vector2>();
                    for(int n=0;n<=10;n++){float a=n*Mathf.PI*2/10;ring.Add(oc+U*Mathf.Cos(a)*rx+V*Mathf.Sin(a)*ry);}
                    string last=g.Polyline(ring.Take(10).ToList(),"Local",RoadType.Main,false);
                    g.Lay(last,ring[0],"Local",RoadType.Main,false,out _);
                    int spokes=3;float start=L()*Mathf.PI*2;
                    for(int n=0;n<spokes;n++){float a=start+n*Mathf.PI*2/spokes;var dir=U*Mathf.Cos(a)*rx+V*Mathf.Sin(a)*ry;Out(oc+dir,oc+dir*(1+LR(1.2f,2.2f)/dir.magnitude),"Street");}
                }
            }
            else if(pattern==2)
            {
                // A curving spine in the upper half with side streets: some end in cul-de-sacs, some bend back as hairpins.
                float bow=LR(-.5f,.5f),sv=hb*.5f;var spine=Line(P(-ha-1,sv),P(ha+1,sv),bow);
                g.Polyline(spine.Take(spine.Count/2+1).Reverse().ToList(),"Street",RoadType.Main,true);
                g.Polyline(spine.Skip(spine.Count/2).ToList(),"Street",RoadType.Main,true);
                for(float u=-ha+LR(.6f,1f);u<ha-.6f;u+=LR(1.25f,1.6f))
                {
                    float t=(u+ha+1)/(2*ha+2);var root=P(u,sv)+V*bow*Mathf.Sin(t*Mathf.PI);
                    float length=LR(1.2f,1.8f);var tip=root+V*length;
                    if(L()<.45f)g.Polyline(new[]{root,tip,tip+U*step,root+U*step},"Local",RoadType.Main,false);
                    else{string end=g.Polyline(new[]{root,root+V*length*.5f,tip},"Local",RoadType.Main,false);if(g.roadsAt[end].Count==1)g.keep.Add(end);}
                }
                // and the spine joins the fabric below
                Out(P(LR(-ha*.5f,ha*.5f),sv),P(LR(-ha*.5f,ha*.5f),-.35f),"Local");
            }
            else if(pattern==3)
            {
                // Wandering lanes that cross the rows.
                int lanes=2+local.Next(2);
                for(int k=0;k<lanes;k++)
                {
                    var p=P(LR(-ha,ha),LR(-hb,hb));float heading=L()*Mathf.PI*2;var list=new List<Vector2>{p};
                    int steps=4+local.Next(3);
                    for(int n=0;n<steps;n++){heading+=LR(-.45f,.45f);p+=new Vector2(Mathf.Cos(heading),Mathf.Sin(heading))*step;list.Add(p);}
                    g.Polyline(list,"Local",RoadType.Main,false);
                }
            }
        }

        // ---- tidy up: unintended dangling ends go, the town must be one piece ----
        bool removed=true;
        while(removed)
        {
            removed=false;
            foreach(var id in g.at.Keys.ToArray())
            {
                if(id=="Store" || g.keep.Contains(id) || g.roadsAt[id].Count>1)continue;
                foreach(var r in g.roadsAt[id].ToArray())g.Remove(r);
                g.at.Remove(id);g.roadsAt.Remove(id);removed=true;
            }
        }
        g.keep.RemoveWhere(id=>!g.at.ContainsKey(id));
        // ---- close the loops: a branch hanging on one road joins a nearby street, so most homes have a way round ----
        HashSet<PatchRoad> CutOf(IEnumerable<PatchRoad> roads=null)
        {
            var temp=ScriptableObject.CreateInstance<MapDefinition>();var ids=new Dictionary<string,PatchRoad>();int n=0;
            foreach(var r in roads??g.roads){string id="C"+(n++);ids[id]=r;temp.Edges.Add(new MapEdge(id,r.a,r.b,1,r.type,1,true));}
            var cutIds=CityValidator.CutRoads(temp);UnityEngine.Object.DestroyImmediate(temp);
            return new HashSet<PatchRoad>(cutIds.Select(id=>ids[id]));
        }
        HashSet<string> Side(string from,PatchRoad banned)
        {
            var seen=new HashSet<string>{from};var queue=new Queue<string>(seen);
            while(queue.Count>0){string cur=queue.Dequeue();foreach(var r in g.roadsAt[cur]){if(r==banned)continue;string o=r.a==cur?r.b:r.a;if(seen.Add(o))queue.Enqueue(o);}}
            return seen;
        }
        // A part of the town apart from the shop's part (cut off entirely, or by one road) joins the rest at its nearest point.
        bool Bridge(HashSet<string> part)
        {
            var best=(a:(string)null,b:(string)null,d:float.PositiveInfinity);
            foreach(var a in part)
            {
                if(g.keep.Contains(a) || g.roadsAt[a].Count>=4)continue;
                foreach(var b in g.at.Keys)
                {
                    if(part.Contains(b) || g.keep.Contains(b) || g.roadsAt[b].Count>=4 || g.roadsAt[a].Any(r=>r.a==b || r.b==b))continue;
                    float d=Vector2.Distance(g.at[a],g.at[b]);
                    if(d<1f || d>=best.d)continue;
                    if(g.at.Any(x=>x.Key!=a && x.Key!=b && PatchGraph.ToSegment(x.Value,g.at[a],g.at[b],out _)<.3f))continue;
                    best=(a,b,d);
                }
            }
            if(best.a==null || best.d>6f)return false;
            int before=g.roads.Count;g.Lay(best.a,g.at[best.b],"Local",RoadType.Main,false,out _);return g.roads.Count>before;
        }
        for(int guard=0;guard<400;guard++)
        {
            var main=Side("Store",null);
            var apart=g.at.Keys.FirstOrDefault(id=>!main.Contains(id));
            if(apart==null)break;
            var part=Side(apart,null);
            if(!Bridge(part))foreach(var id in part.ToList()){foreach(var r in g.roadsAt[id].ToArray())g.Remove(r);g.at.Remove(id);g.roadsAt.Remove(id);g.keep.Remove(id);}
        }
        for(int pass=0;pass<120;pass++)
        {
            bool fixedOne=false;
            foreach(var r in CutOf().OrderBy(r=>(g.at[r.a]+g.at[r.b]).magnitude))
            {
                var side=Side(r.a,r);var far=side.Contains("Store")?Side(r.b,r):side;
                if(far.All(id=>g.keep.Contains(id) || g.roadsAt[id].Count<=1))continue;
                if(Bridge(far)){fixedOne=true;break;}
            }
            if(!fixedOne)break;
        }
        // ---- alleys: a short path where the streets go the long way round ----
        // ---- districts: each node belongs to its nearest neighbourhood; the shop's surroundings are always open ----
        int District(Vector2 p)
        {
            if(p.magnitude<step*1.4f)return -1;
            int best=0;float distance=float.PositiveInfinity;
            for(int i=0;i<centre.Count;i++){float dd=Vector2.Distance(p,centre[i]);if(dd<distance){distance=dd;best=i;}}
            return best;
        }
        var district=g.at.ToDictionary(n=>n.Key,n=>District(n.Value));
        for(int d=0;d<centre.Count;d++)
        {
            var nodes=district.Where(n=>n.Value==d && !g.homes.Contains(n.Key)).Select(n=>n.Key).ToList();
            float RoadDistance(string a,string b)
            {
                var dist=new Dictionary<string,float>{[a]=0};var open=new List<string>{a};
                while(open.Count>0)
                {
                    var cur=open.OrderBy(x=>dist[x]).First();open.Remove(cur);if(cur==b)return dist[cur];if(dist[cur]>8)break;
                    foreach(var r in g.roadsAt[cur]){string o=r.a==cur?r.b:r.a;float nd=dist[cur]+Vector2.Distance(g.at[cur],g.at[o]);if(!dist.ContainsKey(o) || nd<dist[o]){dist[o]=nd;if(!open.Contains(o))open.Add(o);}}
                }
                return float.PositiveInfinity;
            }
            var pairs=nodes.SelectMany((a,i)=>nodes.Skip(i+1).Select(b=>(a,b))).Where(p=>{float l=Vector2.Distance(g.at[p.a],g.at[p.b]);return l>1.2f && l<2.4f;}).OrderBy(p=>Vector2.Distance(g.at[p.a],g.at[p.b])).Take(40);
            foreach(var (a,b) in pairs)
            {
                float straight=Vector2.Distance(g.at[a],g.at[b]);
                if(RoadDistance(a,b)<straight*1.8f)continue;
                bool crosses=g.roads.Any(r=>r.a!=a && r.b!=a && r.a!=b && r.b!=b && SegmentsCross(g.at[a],g.at[b],g.at[r.a],g.at[r.b]));
                bool tight=g.at.Any(n=>n.Key!=a && n.Key!=b && PatchGraph.ToSegment(n.Value,g.at[a],g.at[b],out _)<.45f);
                if(crosses || tight)continue;
                g.Add(a,b,"Local",RoadType.Backstreet);break;
            }
        }

        // ---- homes: on each neighbourhood's streets, preferring roads with a way round ----
        var spacingHome=s.minimumNodeSpacing*1.05f;
        var mapForCut=ScriptableObject.CreateInstance<MapDefinition>();
        foreach(var n in g.at)mapForCut.Nodes.Add(new MapNode(n.Key,n.Value,NodeType.Intersection,false));
        int roadIndex=0;var roadIds=new Dictionary<PatchRoad,string>();
        foreach(var r in g.roads){string id="R"+(roadIndex++);roadIds[r]=id;mapForCut.Edges.Add(new MapEdge(id,r.a,r.b,1,r.type,1,true));}
        var cut=CityValidator.CutRoads(mapForCut);UnityEngine.Object.DestroyImmediate(mapForCut);
        string Home(string id,PatchRoad host,float t=.5f)
        {
            Vector2 a=g.at[host.a],b=g.at[host.b];var p=Vector2.Lerp(a,b,t);
            g.Remove(host);g.at[id]=p;g.roadsAt[id]=new List<PatchRoad>();g.homes.Add(id);
            g.Add(host.a,id,host.kind,host.type);g.Add(id,host.b,host.kind,host.type);return id;
        }
        bool Fits(PatchRoad r)=>r.type==RoadType.Main && Vector2.Distance(g.at[r.a],g.at[r.b])>=spacingHome*2.05f && g.Clear((g.at[r.a]+g.at[r.b])/2,spacingHome) && !g.homes.Contains(r.a) && !g.homes.Contains(r.b);
        // The first two homes, by the shop.
        foreach(string goal in new[]{"Goal1","Goal2"})
        {
            var host=g.roadsAt["Store"].Where(Fits).OrderBy(r=>r.kind=="Avenue"?0:1).FirstOrDefault()??g.roads.Where(r=>district[r.a]==-1 && district[r.b]==-1 && Fits(r)).FirstOrDefault();
            host??=g.roads.Where(r=>g.at[r.a].magnitude<4 && g.at[r.b].magnitude<4 && Fits(r)).OrderBy(r=>(g.at[r.a]+g.at[r.b]).magnitude).FirstOrDefault();
            if(host==null)throw new InvalidOperationException("No room for the first homes by the shop.");
            Home(goal,host);district[goal]=-1;
        }
        var addresses=new List<string[]>();
        for(int d=0;d<centre.Count;d++)
        {
            var list=new List<string>();
            // Roads open once this district opens: both ends here, or one end in a district opened before.
            bool Here(PatchRoad r)=>district.TryGetValue(r.a,out int da) && district.TryGetValue(r.b,out int db) && Mathf.Max(da,db)==d && (da==d || db==d);
            bool Cut(PatchRoad r)=>roadIds.TryGetValue(r,out var id) && cut.Contains(id);
            var hosts=g.roads.Where(r=>Here(r) && Fits(r))
                .OrderBy(r=>Cut(r)?1:0).ThenBy(r=>r.kind=="Avenue"?1:0).ThenBy(_=>Next()).ToList();
            // Roads on a loop first (in the middle, then off-centre on longer roads); a road hanging on its own only
            // when those run out (at most one such home).
            foreach(var host in hosts)
            {
                if(list.Count>=houses)break;
                if(!g.roads.Contains(host) || !Fits(host) || Cut(host))continue;
                string id=$"B{d}_H{list.Count+1}";Home(id,host);district[id]=d;list.Add(id);
            }
            foreach(var host in g.roads.Where(r=>Here(r) && r.type==RoadType.Main && !Cut(r) && !g.homes.Contains(r.a) && !g.homes.Contains(r.b)).OrderBy(_=>Next()).ToList())
            {
                if(list.Count>=houses)break;
                if(!g.roads.Contains(host))continue;
                foreach(float t in new[]{.35f,.65f})
                {
                    var p=Vector2.Lerp(g.at[host.a],g.at[host.b],t);
                    if(Vector2.Distance(p,g.at[host.a])<spacingHome || Vector2.Distance(p,g.at[host.b])<spacingHome || !g.Clear(p,spacingHome))continue;
                    string id=$"B{d}_H{list.Count+1}";Home(id,host,t);district[id]=d;list.Add(id);break;
                }
            }
            int hanging=0;
            foreach(var host in hosts)
            {
                if(list.Count>=houses)break;
                if(!g.roads.Contains(host) || !Fits(host) || !Cut(host) || hanging++>=1)continue;
                string id=$"B{d}_H{list.Count+1}";Home(id,host);district[id]=d;list.Add(id);
            }
            // Still short: a small U-shaped lane beside one of this district's roads, leaving and rejoining it, with the home on it.
            foreach(var host in g.roads.Where(r=>Here(r) && r.type==RoadType.Main && !g.homes.Contains(r.a) && !g.homes.Contains(r.b)).OrderBy(_=>Next()).ToList())
            {
                if(list.Count>=houses)break;
                if(!g.roads.Contains(host))continue;
                Vector2 a=g.at[host.a],b=g.at[host.b],mid=(a+b)/2,along=(b-a).normalized,side=new Vector2(-along.y,along.x);
                foreach(float sign in new[]{1f,-1f})
                {
                    var tip=mid+side*sign*1.05f;
                    if(!g.Clear(tip,spacingHome*1.3f))continue;
                    if(g.roads.Any(r=>r!=host && (SegmentsCross(a,tip,g.at[r.a],g.at[r.b]) || SegmentsCross(tip,b,g.at[r.a],g.at[r.b]) || PatchGraph.ToSegment(tip,g.at[r.a],g.at[r.b],out _)<.55f)))continue;
                    string id=$"B{d}_H{list.Count+1}";g.at[id]=tip;g.roadsAt[id]=new List<PatchRoad>();g.homes.Add(id);
                    g.Add(host.a,id,"Local",RoadType.Main);g.Add(id,host.b,"Local",RoadType.Main);district[id]=d;list.Add(id);break;
                }
            }
            addresses.Add(list.ToArray());
        }

        // ---- the town for this many districts ----
        if(addresses.Take(blockCount).Any(a=>a.Length<houses) || centre.Count<blockCount)throw new InvalidOperationException($"Not enough neighbourhoods or homes: {centre.Count} neighbourhoods, homes "+string.Join(",",addresses.Select(x=>x.Length))+$", nodes {g.at.Count}, roads {g.roads.Count}");
        // Districts open one after another; each brings the roads it needs to join the open town, and a second way in
        // wherever a part with homes hangs on one road. The same order for every size, so a larger town only adds.
        var open=new HashSet<string>(g.at.Keys.Where(id=>district[id]==-1));
        string Other(PatchRoad r,string id)=>r.a==id?r.b:r.a;
        List<string> Path(IEnumerable<string> from,Func<string,bool> goal,PatchRoad banned)
        {
            var dist=new Dictionary<string,float>();var back=new Dictionary<string,string>();var todo=new List<string>();
            foreach(var f in from){dist[f]=0;todo.Add(f);}
            while(todo.Count>0)
            {
                string cur=todo[0];foreach(var t in todo)if(dist[t]<dist[cur])cur=t;todo.Remove(cur);
                if(goal(cur)){var path=new List<string>();for(string x=cur;back.ContainsKey(x);x=back[x])path.Add(x);path.Add(path.Count>0?back[path[^1]]:cur);return path;}
                foreach(var r in g.roadsAt[cur])
                {
                    if(r==banned)continue;string o=Other(r,cur);float nd=dist[cur]+Vector2.Distance(g.at[cur],g.at[o]);
                    if(!dist.TryGetValue(o,out float old) || nd<old){dist[o]=nd;back[o]=cur;if(!todo.Contains(o))todo.Add(o);}
                }
            }
            return null;
        }
        HashSet<string> Reach(PatchRoad banned)
        {
            var seen=new HashSet<string>{"Store"};var queue=new Queue<string>(seen);
            while(queue.Count>0){string cur=queue.Dequeue();foreach(var r in g.roadsAt[cur]){if(r==banned)continue;string o=Other(r,cur);if(open.Contains(o) && seen.Add(o))queue.Enqueue(o);}}
            return seen;
        }
        void Join()
        {
            for(int guard=0;guard<30;guard++)
            {
                var reached=Reach(null);var lost=open.Where(id=>!reached.Contains(id)).ToList();
                if(lost.Count==0)return;
                var path=Path(new[]{lost.OrderBy(id=>g.at[id].magnitude).First()},id=>reached.Contains(id),null);
                if(path==null){foreach(var id in lost)open.Remove(id);return;}
                foreach(var id in path)open.Add(id);
            }
        }
        Join();
        for(int k=0;k<blockCount;k++)
        {
            foreach(var id in g.at.Keys.Where(id=>district[id]==k))open.Add(id);
            Join();
            for(int guard=0;guard<80;guard++)
            {
                // A road whose closure would cut off homes: open the shortest other way between its two ends.
                bool fixedOne=false;
                var openRoads=g.roads.Where(r=>open.Contains(r.a) && open.Contains(r.b)).ToList();
                foreach(var r in CutOf(openRoads).OrderBy(r=>(g.at[r.a]+g.at[r.b]).magnitude))
                {
                    var reached=Reach(r);
                    if(!open.Any(id=>g.homes.Contains(id) && district[id]<=k && !reached.Contains(id)))continue;
                    var path=Path(new[]{r.a},id=>id==r.b,r);
                    if(path==null || path.Count>40 || path.All(open.Contains))continue;
                    foreach(var id in path)open.Add(id);fixedOne=true;break;
                }
                if(!fixedOne)break;
            }
        }
        if(addresses.Take(blockCount).SelectMany(a=>a).Any(id=>!open.Contains(id)))throw new InvalidOperationException("A district could not be joined to the town.");
        var shown=g.roads.Where(r=>open.Contains(r.a) && open.Contains(r.b)).ToList();
        // Streets cut off at the edge of the open town are trimmed back to the last junction.
        var degree=open.ToDictionary(id=>id,id=>shown.Count(r=>r.a==id || r.b==id));
        bool trimmed=true;
        while(trimmed)
        {
            trimmed=false;
            foreach(var id in open.ToArray())
            {
                if(id=="Store" || g.homes.Contains(id) || g.keep.Contains(id) || degree[id]>1)continue;
                foreach(var r in shown.Where(r=>r.a==id || r.b==id).ToArray()){shown.Remove(r);degree[r.a]--;degree[r.b]--;}
                open.Remove(id);trimmed=true;
            }
        }
        foreach(var id in open.OrderBy(id=>id=="Store"?0:1))
            // A home of a district not open yet, passed by a joining road, is shown as a plain point until its district opens.
            map.Nodes.Add(new MapNode(id,g.at[id],g.homes.Contains(id) && district[id]<blockCount?NodeType.DeliveryAddress:NodeType.Intersection,id=="Store"));
        foreach(var r in shown)
            map.Edges.Add(new MapEdge($"{roadIds.GetValueOrDefault(r,"H")}_{(r.kind=="Avenue"?"Avenue_Street":r.kind)}_{r.a}_{r.b}",r.a,r.b,Vector2.Distance(g.at[r.a],g.at[r.b])*s.roadDistanceScale,r.type,1,true));
        for(int d=0;d<blockCount;d++)
        {
            var dr=new System.Random(unchecked(seed*486187739+d*104729+variant*7919+15485863));
            var template=d==0?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Park)??s.templates[0]:
                d==1?s.templates.FirstOrDefault(t=>t.kind==DistrictKind.Riverside)??s.templates[0]:s.templates[dr.Next(s.templates.Length)];
            var points=open.Where(id=>district[id]==d).Select(id=>g.at[id]).ToList();
            city.blocks.Add(new CityBlock{index=d,template=template,addresses=addresses[d],
                area=Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y))});
        }
        city.bridges.AddRange(map.Edges.Where(e=>e.Id.Contains("_Avenue") && e.NodeAId!="Store" && e.NodeBId!="Store").Take(2).Select(e=>e.Id));
        return city;
    }
    private static bool SegmentsCross(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        float Cross(Vector2 x,Vector2 y)=>x.x*y.y-x.y*y.x;
        float d1=Cross(b-a,c-a),d2=Cross(b-a,d-a),d3=Cross(d-c,a-c),d4=Cross(d-c,b-c);
        return d1*d2<0 && d3*d4<0;
    }
}
