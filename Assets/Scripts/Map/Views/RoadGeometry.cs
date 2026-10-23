using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Shared display geometry. Road distance and routing costs remain the authored values.</summary>
public static class RoadGeometry
{
    public static Vector2[] Points(MapManager map,MapEdge edge)
    {
        Vector2 start=map.Nodes.First(n=>n.Id==edge.NodeAId).Position;
        Vector2 end=map.Nodes.First(n=>n.Id==edge.NodeBId).Position;
        return Points(edge,start,end,map.BackstreetVisualOffset);
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
