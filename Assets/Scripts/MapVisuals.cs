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
    private void Create()
    {
        if(created)return;created=true;var theme=UITheme.Active;Camera.main.backgroundColor=theme.background;
        foreach(var area in cityBlocks)Block(area,theme.block,"CityBlock");foreach(var area in parks)Block(area,theme.park,"Park");Route(map.CurrentRouteState);
    }
    private void Block(Rect area,Color color,string name)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=area.center;go.transform.localScale=new Vector3(area.width,area.height,1);
        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=Shape(false);sprite.color=color;sprite.sortingOrder=-20;
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
