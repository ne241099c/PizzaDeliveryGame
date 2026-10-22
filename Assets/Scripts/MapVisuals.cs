using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
/// <summary>Independent decoration data; has no routes, distances or delivery rules.</summary>
[DefaultExecutionOrder(900)]
public sealed class MapVisuals : MonoBehaviour
{
    [SerializeField] private MapManager map;
    [SerializeField] private Rect[] cityBlocks={new Rect(-3.3f,.35f,2,1),new Rect(-1,.4f,2,1.2f),new Rect(1.9f,.4f,1.6f,1),new Rect(-3.3f,-2.2f,3,1.2f),new Rect(0,-2.3f,2.5f,.7f),new Rect(4.4f,-2.8f,.8f,1.6f)};
    [SerializeField] private Rect[] parks={new Rect(-3,-3.4f,2,.8f)};
    private bool created;
    private float previousAspect;
    private void LateUpdate()
    {
        var camera=Camera.main;if(camera==null || !map.IsReady)return;
        float aspect=camera.aspect;
        if(previousAspect>0 && aspect>0 && !Mathf.Approximately(previousAspect,aspect))camera.orthographicSize*=previousAspect/aspect;
        previousAspect=aspect;
        foreach(var c in congestion)if(c.dot!=null)c.dot.enabled=BusinessView.ShowCongestion;
    }
    private readonly List<TextMeshPro> arrows=new();
    private static Sprite circle,square;
    public static Sprite Shape(bool round)
    {
        if(round && circle!=null)return circle;if(!round && square!=null)return square;
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave};
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=x-15.5f,dy=y-15.5f;texture.SetPixel(x,y,new Color(1,1,1,!round || dx*dx+dy*dy<=240?1:0));}texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,32);sprite.hideFlags=HideFlags.DontSave;if(round)circle=sprite;else square=sprite;return sprite;
    }
    private void OnEnable(){map.MapReady+=Create;map.OnRouteChanged+=Route;}
    private void OnDisable(){map.MapReady-=Create;map.OnRouteChanged-=Route;}
    private void Start(){if(map.IsReady)Create();}
    [SerializeField] private Rect[] outerBlocks={new Rect(-7.4f,.5f,2.6f,1.6f),new Rect(-7.4f,-2.2f,2.6f,1.8f),new Rect(5.6f,.5f,2.2f,1.6f),new Rect(5.6f,-2.6f,2.2f,2.2f),new Rect(-3.3f,1.7f,4.6f,1f),new Rect(1.9f,1.7f,3.6f,1f)};
    [SerializeField] private float riverY=-4.6f;
    private readonly List<(MapEdge edge,SpriteRenderer dot)> congestion=new();
    private static Sprite rounded;
    private void Create()
    {
        if(created)return;created=true;var theme=UITheme.Active;Camera.main.backgroundColor=theme.mapGround;
        foreach(var area in cityBlocks)Block(area,theme.mapBlock,"CityBlock");foreach(var area in outerBlocks)Block(area,theme.mapBlock,"CityBlock");
        foreach(var area in parks){Block(area,theme.mapPark,"Park");for(int i=0;i<3;i++)Tree(new Vector2(area.xMin+area.width*(.25f+.25f*i),area.center.y+(i%2==0?.12f:-.12f)),theme.mapTree);}
        River(theme);Congestion(theme);Route(map.CurrentRouteState);
    }
    private static Sprite Rounded()
    {
        if(rounded!=null)return rounded;var tex=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float cx=Mathf.Clamp(x+.5f,16,48),cy=Mathf.Clamp(y+.5f,16,48),d=Mathf.Sqrt((x+.5f-cx)*(x+.5f-cx)+(y+.5f-cy)*(y+.5f-cy))-16;tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));}
        tex.Apply();rounded=Sprite.Create(tex,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));rounded.hideFlags=HideFlags.DontSave;return rounded;
    }
    private void Block(Rect area,Color color,string name)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=area.center;go.transform.localScale=Vector3.one*.5f;
        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=Rounded();sprite.drawMode=SpriteDrawMode.Sliced;sprite.size=area.size*2;sprite.color=color;sprite.sortingOrder=-20;
    }
    private void Tree(Vector2 at,Color color)
    {
        var go=new GameObject("Tree");go.transform.SetParent(transform,false);go.transform.position=at;go.transform.localScale=Vector3.one*.32f;
        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=Shape(true);sprite.color=color;sprite.sortingOrder=-19;
    }
    private void River(UITheme theme)
    {
        var go=new GameObject("River");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
        var road=FindFirstObjectByType<EdgeView>();line.sharedMaterial=road!=null?road.GetComponent<LineRenderer>().sharedMaterial:null;line.useWorldSpace=false;line.positionCount=40;
        for(int i=0;i<40;i++){float x=-12+i*.6f;line.SetPosition(i,new Vector3(x,riverY+Mathf.Sin(i*.45f)*.35f,0));}
        line.startWidth=line.endWidth=1.6f;line.startColor=line.endColor=theme.mapWater;line.sortingOrder=-21;line.numCapVertices=4;
    }
    private void Congestion(UITheme theme)
    {
        var positions=map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);
        foreach(var edge in map.Edges)
        {
            float jam=1-Mathf.Clamp01(edge.TrafficFactor);if(jam<.15f)continue;
            Vector2 a=positions[edge.NodeAId],b=positions[edge.NodeBId];
            if(edge.RoadType==RoadType.Backstreet){var d=(b-a).normalized;var o=new Vector2(-d.y,d.x)*.35f;a+=o;b+=o;}
            int count=Mathf.Max(2,Mathf.RoundToInt((b-a).magnitude*jam*2.4f));
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("Congestion");go.transform.SetParent(transform,false);go.transform.position=Vector2.Lerp(a,b,(i+.5f)/count);go.transform.localScale=Vector3.one*.16f;
                var dot=go.AddComponent<SpriteRenderer>();dot.sprite=Shape(true);dot.color=theme.congestion;dot.sortingOrder=6;congestion.Add((edge,dot));
            }
        }
    }
    private void Route(RouteState state)
    {
        if(!map.IsReady)return;var positions=map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);string current=map.StoreNodeId;int count=0;
        foreach(string id in state.ActiveEdgeIds)
        {
            var edge=map.GetEdge(id);if(!edge.TryTraverse(current,out string next))break;
            while(arrows.Count<=count){var go=new GameObject("RouteArrow");go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshPro>();text.font=UITheme.Active.font;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(.5f,.5f);go.GetComponent<MeshRenderer>().sortingOrder=12;arrows.Add(text);}
            var arrow=arrows[count];Vector2 a=positions[current],b=positions[next];var position=Vector2.Lerp(a,b,count%2==0?.4f:.6f);
            if(edge.RoadType==RoadType.Backstreet){var direction=(positions[edge.NodeBId]-positions[edge.NodeAId]).normalized;position+=new Vector2(-direction.y,direction.x)*.35f;}
            arrow.transform.position=position;arrow.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);arrow.text=">";arrow.fontSize=UITheme.Active.routeArrowSize;arrow.color=UITheme.Active.background;arrow.fontStyle=FontStyles.Bold;count++;current=next;
        }
        for(int i=0;i<arrows.Count;i++)arrows[i].text=i<count?">":"";
    }
}
