using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Shared display geometry. Road distance and routing costs remain the authored values.</summary>
public static class RoadGeometry
{
    public static Vector2[] Points(MapManager map,MapEdge edge)
    {
        if(map.City!=null && map.City.version>=10)return Curved(map,edge);
        Vector2 start=map.Nodes.First(n=>n.Id==edge.NodeAId).Position;
        Vector2 end=map.Nodes.First(n=>n.Id==edge.NodeBId).Position;
        // New towns have separate physical alley routes; their graph geometry is already selectable.
        float offset=map.City!=null && map.City.version>=6?0:map.BackstreetVisualOffset;
        return Points(edge,start,end,offset);
    }

    public static Vector2[] Points(MapEdge edge,Vector2 start,Vector2 end,float backstreetOffset)
    {
        if(edge.RoadType!=RoadType.Backstreet || backstreetOffset<=0)return new[]{start,end};
        Vector2 delta=end-start;
        float length=delta.magnitude;
        if(length<=Mathf.Epsilon)return new[]{start,end};
        Vector2 normal=new Vector2(-delta.y,delta.x)/length;
        Vector2 offset=normal*Mathf.Min(backstreetOffset,length*.4f);
        // Keep both junctions attached. The separate middle section is easier to
        // point at than a second stroke overlapping a main road.
        return new[]{start,start+delta*.2f+offset,start+delta*.8f+offset,end};
    }

    // ---- v10: roads drawn as smooth curves through their junctions and homes (the graph stays straight) ----
    private static readonly Dictionary<MapEdge,Vector2[]> curves=new();
    private static readonly Dictionary<string,List<MapEdge>> roadsAt=new();
    private static readonly Dictionary<string,Vector2> nodeAt=new();
    private static readonly HashSet<MapEdge> known=new();
    private const int CurveSamples=10;
    private static void Index(MapManager map)
    {
        curves.Clear();roadsAt.Clear();nodeAt.Clear();known.Clear();
        foreach(var n in map.Nodes)nodeAt[n.Id]=n.Position;
        foreach(var e in map.Edges)
        {
            known.Add(e);
            foreach(var id in new[]{e.NodeAId,e.NodeBId}){if(!roadsAt.TryGetValue(id,out var list))roadsAt[id]=list=new List<MapEdge>();list.Add(e);}
        }
    }
    /// <summary>The node that carries the road on straight through <paramref name="here"/>, coming from <paramref name="from"/>.</summary>
    private static Vector2? Continuation(MapEdge edge,string here,string from)
    {
        Vector2 at=nodeAt[here],incoming=at-nodeAt[from];Vector2? best=null;float bestAngle=50;
        foreach(var other in roadsAt[here])
        {
            if(other==edge)continue;
            string next=other.NodeAId==here?other.NodeBId:other.NodeAId;
            float angle=Vector2.Angle(incoming,nodeAt[next]-at);
            if(angle<bestAngle){bestAngle=angle;best=nodeAt[next];}
        }
        return best;
    }
    private static Vector2[] Curved(MapManager map,MapEdge edge)
    {
        if(!known.Contains(edge))Index(map);
        if(curves.TryGetValue(edge,out var cached))return cached;
        Vector2 a=nodeAt[edge.NodeAId],b=nodeAt[edge.NodeBId];
        Vector2 before=Continuation(edge,edge.NodeAId,edge.NodeBId)??a-(b-a),after=Continuation(edge,edge.NodeBId,edge.NodeAId)??b+(b-a);
        // Catmull-Rom through a and b; the ends keep the neighbouring roads' direction.
        Vector2 t0=(b-before)*.5f,t1=(after-a)*.5f;
        var points=new Vector2[CurveSamples+1];
        for(int i=0;i<=CurveSamples;i++)
        {
            float t=(float)i/CurveSamples,t2=t*t,t3=t2*t;
            points[i]=(2*t3-3*t2+1)*a+(t3-2*t2+t)*t0+(-2*t3+3*t2)*b+(t3-t2)*t1;
        }
        curves[edge]=points;return points;
    }

    public static Vector2 PointAt(IReadOnlyList<Vector2> points,float progress)
    {
        if(points==null || points.Count==0)return Vector2.zero;
        float total=0;
        for(int i=1;i<points.Count;i++)total+=Vector2.Distance(points[i-1],points[i]);
        float remaining=Mathf.Clamp01(progress)*total;
        for(int i=1;i<points.Count;i++)
        {
            float length=Vector2.Distance(points[i-1],points[i]);
            if(remaining<=length)return Vector2.Lerp(points[i-1],points[i],length>0?remaining/length:0);
            remaining-=length;
        }
        return points[points.Count-1];
    }

    public static float DistanceToScreenPoint(Camera camera,IReadOnlyList<Vector2> points,Vector2 point)
    {
        float closest=float.PositiveInfinity;
        for(int i=1;i<points.Count;i++)
        {
            Vector2 a=camera.WorldToScreenPoint(points[i-1]);
            Vector2 b=camera.WorldToScreenPoint(points[i]);
            Vector2 delta=b-a;
            float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
            closest=Mathf.Min(closest,Vector2.Distance(point,a+delta*t));
        }
        return closest;
    }
}
