using UnityEditor;
using UnityEngine;

/// <summary>Editor-only seed preview; never replaces the player's map or save.</summary>
public sealed class CityGenerationWindow:EditorWindow
{
    private int seed=1401,day=1;private GeneratedCity city;private readonly CityGenerationSettings settings=new();
    [MenuItem("Tools/Pizza Delivery/City Generator")]
    public static void Open()=>GetWindow<CityGenerationWindow>("街の生成");
    private void OnDisable(){if(city!=null)DestroyImmediate(city.map);}
    private void OnGUI()
    {
        seed=EditorGUILayout.IntField("シード",seed);day=EditorGUILayout.IntSlider("日数",day,1,30);
        if(GUILayout.Button("生成・検査")){if(city!=null)DestroyImmediate(city.map);city=CityGenerator.Generate(seed,settings.Blocks(day),settings);}
        if(city==null)return;
        GUILayout.Label(city.metrics.Summary);
        GUILayout.Label("検査: "+string.Join(" / ",city.metrics.failures));
        GUILayout.Label($"再試行 {city.attempt} / フォールバック {city.fallback}");
        var rect=GUILayoutUtility.GetRect(position.width-20,position.height-160);EditorGUI.DrawRect(rect,new Color(.92f,.9f,.84f));
        var bounds=new Bounds(city.map.Nodes[0].Position,Vector3.zero);foreach(var node in city.map.Nodes)bounds.Encapsulate(node.Position);
        float scale=Mathf.Min((rect.width-24)/Mathf.Max(1,bounds.size.x),(rect.height-24)/Mathf.Max(1,bounds.size.y));
        Vector3 Point(MapNode n)=>new Vector3(rect.center.x+(n.Position.x-bounds.center.x)*scale,rect.center.y-(n.Position.y-bounds.center.y)*scale);
        Handles.BeginGUI();
        foreach(var e in city.map.Edges)
        {
            var a=city.map.Nodes.Find(n=>n.Id==e.NodeAId);
            var b=city.map.Nodes.Find(n=>n.Id==e.NodeBId);
            Handles.color=e.RoadType==RoadType.Main?new Color(.85f,.6f,.2f):Color.white;
            Handles.DrawAAPolyLine(e.RoadType==RoadType.Main?4:2,Point(a),Point(b));
        }
        Handles.EndGUI();
        foreach(var n in city.map.Nodes)if(n.IsStore || n.NodeType==NodeType.DeliveryAddress)
        {
            var p=Point(n);
            EditorGUI.DrawRect(new Rect(p.x-3,p.y-3,6,6),n.IsStore?Color.red:new Color(.3f,.6f,.3f));
        }
    }
}
