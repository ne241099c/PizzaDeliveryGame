using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class CityMetrics
{
    public int nodes,edges,destinations,shortcutDestinations,nearPairs,bridges,oneWay,near,middle,far;
    public float totalDistance,deadEndFraction,minimumSaving,maximumSaving,minimumSpacing;
    public readonly List<string> failures=new();
    public string Summary=>$"道{edges}本 / 配達先{destinations} / 近道候補{shortcutDestinations} / 近接ペア{nearPairs}";
}
/// <summary>Uses the production path search, including one-way traversal. No alternate routing algorithm.</summary>
public static class CityValidator
{
    public static CityMetrics Measure(GeneratedCity city,CityGenerationSettings s)
    {
        var map=city.map;var m=new CityMetrics{nodes=map.Nodes.Count,edges=map.Edges.Count,totalDistance=map.Edges.Sum(e=>e.Distance),bridges=city.bridges.Count,oneWay=map.Edges.Count(e=>!e.Bidirectional),minimumSaving=1,minimumSpacing=float.PositiveInfinity};
        void Fail(bool ok,string name){if(!ok && !m.failures.Contains(name))m.failures.Add(name);}
        Fail(map.Nodes.Count(n=>n.IsStore)==1,"store");
        Fail(map.Nodes.Select(n=>n.Id).Distinct().Count()==map.Nodes.Count && map.Edges.Select(e=>e.Id).Distinct().Count()==map.Edges.Count,"identity");
        if(m.failures.Count>0)return m;
        string store=map.Nodes.Single(n=>n.IsStore).Id;var ids=new HashSet<string>(map.Nodes.Select(n=>n.Id));
        Fail(map.Edges.All(e=>ids.Contains(e.NodeAId) && ids.Contains(e.NodeBId) && e.Distance>0 && e.TrafficFactor>0 && !float.IsInfinity(e.Distance) && !float.IsNaN(e.Distance)),"roads");if(m.failures.Count>0)return m;
        var goals=map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore).ToArray();m.destinations=goals.Length;
        float Cost(MapEdge e)=>e.Distance/e.TrafficFactor;
        float Distance(string from,string to,IEnumerable<MapEdge> edges,Func<MapEdge,float> cost)
        {var list=edges.ToArray();var path=RoadPathfinder.FindShortestPath(list,from,to,cost);return path==null?float.PositiveInfinity:path.Sum(id=>cost(list.First(e=>e.Id==id)));}
        var distances=new List<float>();
        foreach(var node in map.Nodes)
        {Fail(!float.IsInfinity(Distance(store,node.Id,map.Edges,Cost)) && !float.IsInfinity(Distance(node.Id,store,map.Edges,Cost)),"connectivity/return");}
        foreach(var goal in goals)
        {
            float all=Distance(store,goal.Id,map.Edges,Cost),main=Distance(store,goal.Id,map.Edges.Where(e=>e.RoadType==RoadType.Main),Cost);distances.Add(all);
            if(!float.IsInfinity(main) && main>0){float saving=1-all/main;if(saving>=s.minimumShortcutSaving){m.shortcutDestinations++;m.minimumSaving=Mathf.Min(m.minimumSaving,saving);m.maximumSaving=Mathf.Max(m.maximumSaving,saving);}}
        }
        m.deadEndFraction=(float)map.Nodes.Count(n=>map.Edges.Count(e=>e.NodeAId==n.Id || e.NodeBId==n.Id)<=1)/Mathf.Max(1,m.nodes);
        Fail(m.deadEndFraction<=s.maximumDeadEndFraction,"dead ends");
        Fail(m.shortcutDestinations>0 && m.shortcutDestinations<m.destinations && m.maximumSaving<=s.maximumShortcutSaving,"shortcut value/mix");
        foreach(var edge in map.Edges.Where(e=>e.RoadType==RoadType.Main && e.TrafficFactor<1))
            Fail(!float.IsInfinity(Distance(edge.NodeAId,edge.NodeBId,map.Edges.Where(e=>e!=edge),Cost)),"congestion detours");
        Fail(m.edges>=s.minimumEdges && m.edges<=s.maximumEdges && m.destinations>=s.minimumDestinations && m.destinations<=s.maximumDestinations && m.totalDistance<=s.maximumTotalDistance,"scale");
        if(distances.Count>0 && distances.All(d=>!float.IsInfinity(d)))
        {
            float min=distances.Min(),span=Mathf.Max(.001f,distances.Max()-min);
            foreach(float distance in distances){float fraction=(distance-min)/span;if(fraction<1f/3)m.near++;else if(fraction<2f/3)m.middle++;else m.far++;}
        }
        Fail(m.near>0 && m.middle>0 && m.far>0,"distance distribution");
        for(int a=0;a<goals.Length;a++)for(int b=a+1;b<goals.Length;b++)if(Distance(goals[a].Id,goals[b].Id,map.Edges,Cost)<=s.nearbyDistance)m.nearPairs++;
        Fail(m.nearPairs>0,"batch opportunity");
        Fail(m.bridges>=2 && city.bridges.All(id=>map.Edges.Any(e=>e.Id==id)),"river bridges");
        for(int a=0;a<map.Nodes.Count;a++)for(int b=a+1;b<map.Nodes.Count;b++)m.minimumSpacing=Mathf.Min(m.minimumSpacing,Vector2.Distance(map.Nodes[a].Position,map.Nodes[b].Position));
        Fail(m.minimumSpacing>=s.minimumNodeSpacing,"display spacing");
        return m;
    }
}
