using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class CityMetrics
{
    public int nodes,edges,destinations,shortcutDestinations,nearPairs,bridges,oneWay,near,middle,far,multiRouteHomes;
    public float totalDistance,deadEndFraction,minimumSaving,maximumSaving,minimumSpacing;
    public readonly List<string> failures=new();
    public string Summary=>$"道{edges}本 / 配達先{destinations} / 近道候補{shortcutDestinations} / 近接ペア{nearPairs}";
}
/// <summary>Uses the production path search, including one-way traversal. No alternate routing algorithm.</summary>
public static class CityValidator
{
    public static CityMetrics Measure(GeneratedCity city,CityGenerationSettings s)
    {
        var map=city.map;
        var m=new CityMetrics{nodes=map.Nodes.Count,edges=map.Edges.Count,totalDistance=map.Edges.Sum(e=>e.Distance),bridges=city.bridges.Count,oneWay=map.Edges.Count(e=>!e.Bidirectional),minimumSaving=1,minimumSpacing=float.PositiveInfinity};
        void Fail(bool ok,string name){if(!ok && !m.failures.Contains(name))m.failures.Add(name);}
        Fail(map.Nodes.Count(n=>n.IsStore)==1,"store");
        Fail(map.Nodes.Select(n=>n.Id).Distinct().Count()==map.Nodes.Count && map.Edges.Select(e=>e.Id).Distinct().Count()==map.Edges.Count,"identity");
        if(m.failures.Count>0)return m;
        string store=map.Nodes.Single(n=>n.IsStore).Id;var ids=new HashSet<string>(map.Nodes.Select(n=>n.Id));
        Fail(map.Edges.All(e=>ids.Contains(e.NodeAId) && ids.Contains(e.NodeBId) && e.Distance>0 && e.TrafficFactor>0 && !float.IsInfinity(e.Distance) && !float.IsNaN(e.Distance)),"roads");
        if(m.failures.Count>0)return m;
        var goals=map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore).ToArray();m.destinations=goals.Length;
        float Cost(MapEdge e)=>e.Distance/e.TrafficFactor;
        var byId=map.Edges.ToDictionary(e=>e.Id);
        float Distance(string from,string to,IEnumerable<MapEdge> edges,Func<MapEdge,float> cost)
        {
            var path=RoadPathfinder.FindShortestPath(edges as IReadOnlyList<MapEdge> ?? edges.ToArray(),from,to,cost);
            return path==null?float.PositiveInfinity:path.Sum(id=>cost(byId[id]));
        }
        var distances=new List<float>();
        foreach(var node in map.Nodes)
        {
            Fail(!float.IsInfinity(Distance(store,node.Id,map.Edges,Cost)) && !float.IsInfinity(Distance(node.Id,store,map.Edges,Cost)),"connectivity/return");
        }
        foreach(var goal in goals)
        {
            float all=Distance(store,goal.Id,map.Edges,Cost),main=Distance(store,goal.Id,map.Edges.Where(e=>e.RoadType==RoadType.Main),Cost);
            distances.Add(all);
            if(!float.IsInfinity(main) && main>0)
            {
                float saving=1-all/main;
                if(saving>=s.minimumShortcutSaving)
                {
                    m.shortcutDestinations++;
                    m.minimumSaving=Mathf.Min(m.minimumSaving,saving);
                    m.maximumSaving=Mathf.Max(m.maximumSaving,saving);
                }
            }
        }
        m.deadEndFraction=(float)map.Nodes.Count(n=>map.Edges.Count(e=>e.NodeAId==n.Id || e.NodeBId==n.Id)<=1)/Mathf.Max(1,m.nodes);
        Fail(m.deadEndFraction<=s.maximumDeadEndFraction,"dead ends");
        Fail(m.shortcutDestinations>0 && m.shortcutDestinations<m.destinations && m.maximumSaving<=s.maximumShortcutSaving,"shortcut value/mix");
        foreach(var edge in map.Edges.Where(e=>e.RoadType==RoadType.Main && e.TrafficFactor<1))
            Fail(!float.IsInfinity(Distance(edge.NodeAId,edge.NodeBId,map.Edges.Where(e=>e!=edge),Cost)),"congestion detours");
        Fail(m.edges>=s.minimumEdges && m.edges<=(city.version>=10?Mathf.Max(s.maximumEdges,s.newTownMaximumEdges):s.maximumEdges) && m.destinations>=s.minimumDestinations && m.destinations<=s.maximumDestinations && m.totalDistance<=s.maximumTotalDistance,"scale");
        if(distances.Count>0 && distances.All(d=>!float.IsInfinity(d)))
        {
            float min=distances.Min(),span=Mathf.Max(.001f,distances.Max()-min);
            foreach(float distance in distances)
            {
                float fraction=(distance-min)/span;
                if(fraction<1f/3)m.near++;
                else if(fraction<2f/3)m.middle++;
                else m.far++;
            }
        }
        Fail(m.near>0 && m.middle>0 && m.far>0,"distance distribution");
        // A route costs at least (cheapest cost per straight-line unit) x (straight-line distance), so pairs that are
        // too far apart even in a straight line cannot be near and need no search. The count is the same.
        var at=map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);
        float perUnit=map.Edges.Min(e=>{float length=Vector2.Distance(at[e.NodeAId],at[e.NodeBId]);return length>0?Cost(e)/length:0;});
        for(int a=0;a<goals.Length;a++)for(int b=a+1;b<goals.Length;b++)
        {
            if(perUnit*Vector2.Distance(goals[a].Position,goals[b].Position)>s.nearbyDistance)continue;
            if(Distance(goals[a].Id,goals[b].Id,map.Edges,Cost)<=s.nearbyDistance)m.nearPairs++;
        }
        Fail(m.nearPairs>0,"batch opportunity");
        Fail(m.bridges>=2 && city.bridges.All(id=>map.Edges.Any(e=>e.Id==id)),"river bridges");
        for(int a=0;a<map.Nodes.Count;a++)for(int b=a+1;b<map.Nodes.Count;b++)m.minimumSpacing=Mathf.Min(m.minimumSpacing,Vector2.Distance(map.Nodes[a].Position,map.Nodes[b].Position));
        Fail(m.minimumSpacing>=s.minimumNodeSpacing,"display spacing");
        // Homes still reachable when any single road is closed: they share a loop-connected part with the store.
        var cut=CutRoads(map);var reached=new HashSet<string>{store};var queue=new Queue<string>(reached);
        while(queue.Count>0)
        {
            string from=queue.Dequeue();
            foreach(var e in map.Edges)
            {
                if(cut.Contains(e.Id) || e.NodeAId!=from && e.NodeBId!=from)continue;
                string next=e.NodeAId==from?e.NodeBId:e.NodeAId;if(reached.Add(next))queue.Enqueue(next);
            }
        }
        m.multiRouteHomes=goals.Count(g=>reached.Contains(g.Id));
        if(city.version>=9)Fail(m.multiRouteHomes>=goals.Length*s.minimumMultiRouteShare,"multiple routes");
        return m;
    }
    /// <summary>Roads whose closure would split the town in two (graph bridges, direction ignored).</summary>
    public static HashSet<string> CutRoads(MapDefinition map)
    {
        var next=new Dictionary<string,List<(string to,string edge)>>();
        foreach(var e in map.Edges)
        {
            if(!next.ContainsKey(e.NodeAId))next[e.NodeAId]=new();
            if(!next.ContainsKey(e.NodeBId))next[e.NodeBId]=new();
            next[e.NodeAId].Add((e.NodeBId,e.Id));next[e.NodeBId].Add((e.NodeAId,e.Id));
        }
        var found=new Dictionary<string,int>();var low=new Dictionary<string,int>();var cut=new HashSet<string>();int time=0;
        foreach(string start in next.Keys)
        {
            if(found.ContainsKey(start))continue;
            found[start]=low[start]=time++;
            var stack=new Stack<(string node,string via,int index)>();stack.Push((start,null,0));
            while(stack.Count>0)
            {
                var (node,via,index)=stack.Pop();var list=next[node];
                if(index<list.Count)
                {
                    stack.Push((node,via,index+1));
                    var (to,edge)=list[index];if(edge==via)continue;
                    if(!found.ContainsKey(to)){found[to]=low[to]=time++;stack.Push((to,edge,0));}
                    else low[node]=Math.Min(low[node],found[to]);
                    continue;
                }
                // Finished: the frame below is the parent's.
                if(via==null || stack.Count==0)continue;
                string parent=stack.Peek().node;low[parent]=Math.Min(low[parent],low[node]);
                if(low[node]>found[parent])cut.Add(via);
            }
        }
        return cut;
    }
}
