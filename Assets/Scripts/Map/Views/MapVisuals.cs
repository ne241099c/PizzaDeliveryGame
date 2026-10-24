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
    private Transform decorationRoot;
    private float previousAspect;
    private void LateUpdate()
    {
        var camera=Camera.main;if(camera==null || !map.IsReady)return;
        float aspect=camera.aspect;
        if(previousAspect>0 && aspect>0 && !Mathf.Approximately(previousAspect,aspect))camera.orthographicSize*=previousAspect/aspect;
        previousAspect=aspect;
        float arrowSize=UITheme.Active.mapRouteArrowPixels*10*camera.orthographicSize*2/Mathf.Max(1,camera.pixelHeight);
        foreach(var arrow in arrows)if(arrow!=null){arrow.fontSize=arrowSize;arrow.enabled=!BusinessView.HideMapArrows;}
        foreach(var c in congestion)if(c.dot!=null)c.dot.enabled=BusinessView.ShowCongestion && Visible(camera,c.dot.bounds);
        float px=camera.orthographicSize*2/Mathf.Max(1,camera.pixelHeight);
        foreach(var pair in districtLabels)
        {
            if(pair.label==null)continue;float pixels=pair.area.width/px;
            pair.label.enabled=pixels>=UITheme.Active.cityLabelMinimumBlockPixels && Visible(camera,new Bounds(pair.area.center,pair.area.size));
            pair.label.fontSize=UITheme.Active.captionSize*10*px;
            pair.label.rectTransform.sizeDelta=new Vector2(pair.area.width*.7f,UITheme.Active.captionSize*2*px);
        }
        foreach(var sprite in detailSprites)if(sprite!=null)sprite.enabled=Visible(camera,sprite.bounds) && (map.City==null || (map.City.version>=7?UITheme.Active.mapBuildingSize/px>=3:map.CitySettings.blockSize/px>=UITheme.Active.cityDetailMinimumBlockPixels));
    }
    private static bool Visible(Camera camera,Bounds bounds)
    {
        var a=camera.WorldToViewportPoint(bounds.min);var b=camera.WorldToViewportPoint(bounds.max);return b.x>=0 && a.x<=1 && b.y>=0 && a.y<=1;
    }
    private readonly List<TextMeshPro> arrows=new();
    private static Sprite circle,square;
    public static Sprite Shape(bool round)
    {
        if(round && circle!=null)return circle;if(!round && square!=null)return square;
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave};
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float dx=x-15.5f,dy=y-15.5f;
            texture.SetPixel(x,y,new Color(1,1,1,!round || dx*dx+dy*dy<=240?1:0));
        }
        texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,32);
        sprite.hideFlags=HideFlags.DontSave;
        if(round)circle=sprite;
        else square=sprite;
        return sprite;
    }
    private void OnEnable(){map.MapReady+=Create;map.OnRouteChanged+=Route;}
    private void OnDisable(){map.MapReady-=Create;map.OnRouteChanged-=Route;}
    private void Start(){if(map.IsReady)Create();}
    [SerializeField] private Rect[] outerBlocks={new Rect(-7.4f,.5f,2.6f,1.6f),new Rect(-7.4f,-2.2f,2.6f,1.8f),new Rect(5.6f,.5f,2.2f,1.6f),new Rect(5.6f,-2.6f,2.2f,2.2f),new Rect(-3.3f,1.7f,4.6f,1f),new Rect(1.9f,1.7f,3.6f,1f)};
    [SerializeField] private float riverY=-4.6f;
    private readonly List<(MapEdge edge,SpriteRenderer dot)> congestion=new();
    private readonly List<(TextMeshPro label,Rect area)> districtLabels=new();
    private readonly List<SpriteRenderer> detailSprites=new();
    private int shownSeed,shownBlocks;
    private void GeneratedDetails(UITheme theme)
    {
        var previous=shownSeed==map.City.seed?shownBlocks:0;shownSeed=map.City.seed;shownBlocks=map.City.blocks.Count;
        foreach(var block in map.City.blocks)
        {
            var area=block.area;
            var labelObject=new GameObject("DistrictName_"+block.index);
            labelObject.transform.SetParent(decorationRoot,false);
            labelObject.transform.position=new Vector3(area.x+area.width*.28f,area.center.y,0);
            var label=labelObject.AddComponent<TextMeshPro>();
            label.font=theme.font;
            label.text=block.template.title+" "+(block.index+1);
            label.color=theme.inkSub;
            label.alignment=TextAlignmentOptions.Center;
            label.fontStyle=FontStyles.Bold;
            label.GetComponent<MeshRenderer>().sortingOrder=-10;
            districtLabels.Add((label,area));
            var roadNameObject=new GameObject("RoadName_"+block.index);
            roadNameObject.transform.SetParent(decorationRoot,false);
            roadNameObject.transform.position=new Vector3(area.center.x,area.yMax+.5f,0);
            var roadName=roadNameObject.AddComponent<TextMeshPro>();
            roadName.font=theme.font;
            roadName.text=block.template.title+" "+(block.index+1)+"号通り";
            roadName.color=theme.inkSub;
            roadName.alignment=TextAlignmentOptions.Center;
            roadName.GetComponent<MeshRenderer>().sortingOrder=-10;
            districtLabels.Add((roadName,area));
            if(block.template.river)
            {
                float x=area.x+area.width*.3f;
                var river=new GameObject("DistrictRiver").AddComponent<LineRenderer>();
                river.transform.SetParent(decorationRoot,false);
                river.positionCount=2;
                river.SetPosition(0,new Vector3(x,area.yMin-.3f,0));
                river.SetPosition(1,new Vector3(x,area.yMax+.3f,0));
                var road=FindFirstObjectByType<EdgeView>();
                if(road!=null)river.sharedMaterial=road.GetComponent<LineRenderer>().sharedMaterial;
                river.startWidth=river.endWidth=.6f;
                river.startColor=river.endColor=theme.mapWater;
                river.sortingOrder=-18;
                foreach(float y in new[]{area.yMin,area.yMax})Block(new Rect(x-.4f,y-.25f,.8f,.5f),theme.paper,"Bridge");
            }
            if(block.template.buildings)
                for(int i=0;i<3;i++)Block(new Rect(area.x+.65f,area.y+.6f+i*.8f,.7f,.4f),theme.paperDeep,"DistrictBuilding");
            if(previous>0 && block.index>=previous)
            {
                if(districts.TryGetValue(block.index,out var renderer) && renderer!=null)StartCoroutine(RevealDistrict(renderer,theme.cityExpansionFadeSeconds));
            }
        }
    }
    private System.Collections.IEnumerator RevealDistrict(SpriteRenderer sprite,float seconds)
    {
        Color color=sprite.color;
        for(float t=0;t<seconds;t+=Time.unscaledDeltaTime)
        {
            if(sprite==null)yield break;
            var c=color;
            c.a=Mathf.Clamp01(t/seconds);
            sprite.color=c;
            yield return null;
        }
        if(sprite!=null)sprite.color=color;
    }

    /// <summary>Roadside plots follow the new street graph rather than drawing rectangular districts.</summary>
    private void NetworkDetails(UITheme theme)
    {
        var positions=map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);
        float spacing=Mathf.Max(.5f,theme.mapBuildingSpacing),setback=Mathf.Max(.2f,theme.mapBuildingSetback),size=Mathf.Max(.1f,theme.mapBuildingSize);
        float Distance(Vector2 point,Vector2 a,Vector2 b)
        {
            var delta=b-a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(point,a+delta*t);
        }
        uint Hash(string text){uint hash=2166136261;foreach(char c in text)hash=unchecked((hash^c)*16777619);return hash;}
        var plots=new List<Vector2>();
        // Along the drawn road (curved in a v10 town), checked against every drawn road.
        var drawn=map.Edges.ToDictionary(e=>e,e=>RoadGeometry.Points(map,e));
        float ToRoad(Vector2 point,Vector2[] road){float best=float.PositiveInfinity;for(int i=1;i<road.Length;i++)best=Mathf.Min(best,Distance(point,road[i-1],road[i]));return best;}
        foreach(var edge in map.Edges)
        {
            var road=drawn[edge];float length=0;for(int i=1;i<road.Length;i++)length+=Vector2.Distance(road[i-1],road[i]);
            if(length<spacing)continue;
            int count=Mathf.Max(1,Mathf.FloorToInt(length/spacing));
            for(int i=0;i<count;i++)foreach(int side in new[]{-1,1})
            {
                uint hash=Hash(edge.Id+"/"+i+"/"+side);float jitter=((hash%101)/100f-.5f)*spacing*.3f;
                float t=Mathf.Clamp01(((i+.5f)*spacing+jitter)/length);
                Vector2 delta=RoadGeometry.PointAt(road,Mathf.Min(1,t+.02f))-RoadGeometry.PointAt(road,Mathf.Max(0,t-.02f));
                var normal=new Vector2(-delta.y,delta.x).normalized;
                Vector2 point=RoadGeometry.PointAt(road,t)+normal*setback*side;
                if(Vector2.Distance(point,positions[map.StoreNodeId])<1 || plots.Any(p=>Vector2.Distance(p,point)<size*1.6f))continue;
                if(map.Edges.Any(other=>ToRoad(point,drawn[other])<size*.7f))continue;
                plots.Add(point);Vector2 dimensions=new Vector2(size*(.85f+(hash%7)*.05f),size*(1+(hash%5)*.08f));
                var building=Block(new Rect(point-dimensions*.5f,dimensions),Color.Lerp(theme.mapBuildingFill,theme.paper,(hash%5)*.08f),"NeighborhoodBuilding");
                building.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                building.sortingOrder=-8;detailSprites.Add(building);
            }
        }
        foreach(var district in map.City.blocks)
        {
            Vector2 center=district.addresses.Where(positions.ContainsKey).Select(id=>positions[id]).Aggregate(Vector2.zero,(sum,p)=>sum+p)/Mathf.Max(1,district.addresses.Length);
            var label=new GameObject("DistrictName_"+district.index).AddComponent<TextMeshPro>();label.transform.SetParent(decorationRoot,false);label.transform.position=center;
            label.font=theme.font;label.text=district.template.title;label.color=theme.inkSub;label.alignment=TextAlignmentOptions.Center;
            label.GetComponent<MeshRenderer>().sortingOrder=-10;districtLabels.Add((label,district.area));
        }
        // Keep bridge markers attached to the actual selected roads; no rectangular river patches.
        foreach(string id in map.City.bridges.Distinct())
        {
            var edge=map.GetEdge(id);Vector2 a=positions[edge.NodeAId],b=positions[edge.NodeBId],center=(a+b)*.5f;
            var bridge=Block(new Rect(center-new Vector2(.22f,.17f),new Vector2(.44f,.34f)),theme.paperDeep,"Bridge");
            bridge.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);bridge.sortingOrder=-2;
        }
    }

    private static Sprite rounded;
    private void Create()
    {
        if(decorationRoot!=null)
        {
            decorationRoot.gameObject.SetActive(false);
            Destroy(decorationRoot.gameObject);
        }
        decorationRoot=new GameObject("CityDecoration").transform;
        decorationRoot.SetParent(transform,false);
        arrows.Clear();
        congestion.Clear();
        districtLabels.Clear();
        districts.Clear();
        detailSprites.Clear();
        var theme=UITheme.Active;
        Camera.main.backgroundColor=theme.mapGround;
        if(map.City!=null && map.City.version>=7)
        {
            NetworkDetails(theme);
        }
        else if(map.City!=null)
        {
            foreach(var block in map.City.blocks)
            {
                var a=block.area;
                var inset=new Rect(a.x+.5f,a.y+.5f,a.width-1,a.height-1);
                bool green=block.template.trees;
                districts[block.index]=Block(inset,green?theme.mapPark:theme.mapBlock,"District_"+block.index);
                if(green)for(int i=0;i<3;i++)Tree(inset.center+new Vector2((i-1)*.6f,.3f),theme.mapTree);
            }
        }
        else
        {
            foreach(var area in cityBlocks)Block(area,theme.mapBlock,"CityBlock");
            foreach(var area in outerBlocks)Block(area,theme.mapBlock,"CityBlock");
        }
        foreach(var area in map.City!=null && map.City.version>=7?System.Array.Empty<Rect>():parks)
        {
            Block(area,theme.mapPark,"Park");
            for(int i=0;i<3;i++)Tree(new Vector2(area.xMin+area.width*(.25f+.25f*i),area.center.y+(i%2==0?.12f:-.12f)),theme.mapTree);
        }
        if(map.City==null)River(theme);else if(map.City.version<7)GeneratedDetails(theme);
        Congestion(theme);Route(map.CurrentRouteState);
    }
    private static Sprite Rounded()
    {
        if(rounded!=null)return rounded;
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float cx=Mathf.Clamp(x+.5f,16,48),cy=Mathf.Clamp(y+.5f,16,48),d=Mathf.Sqrt((x+.5f-cx)*(x+.5f-cx)+(y+.5f-cy)*(y+.5f-cy))-16;
            tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));
        }
        tex.Apply();
        rounded=Sprite.Create(tex,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        rounded.hideFlags=HideFlags.DontSave;
        return rounded;
    }
    /// <summary>Each district's ground block, by block index (the newest ones fade in).</summary>
    private readonly System.Collections.Generic.Dictionary<int,SpriteRenderer> districts=new();
    private SpriteRenderer Block(Rect area,Color color,string name)
    {
        var go=new GameObject(name);
        go.transform.SetParent(decorationRoot,false);
        go.transform.position=area.center;
        go.transform.localScale=Vector3.one*.5f;
        var sprite=go.AddComponent<SpriteRenderer>();
        sprite.sprite=Rounded();
        sprite.drawMode=SpriteDrawMode.Sliced;
        sprite.size=area.size*2;
        sprite.color=color;
        sprite.sortingOrder=-20;
        return sprite;
    }
    private void Tree(Vector2 at,Color color)
    {
        var go=new GameObject("Tree");go.transform.SetParent(decorationRoot,false);go.transform.position=at;go.transform.localScale=Vector3.one*.32f;
        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=Shape(true);sprite.color=color;sprite.sortingOrder=-19;detailSprites.Add(sprite);
    }
    private void River(UITheme theme)
    {
        var go=new GameObject("River");go.transform.SetParent(decorationRoot,false);var line=go.AddComponent<LineRenderer>();
        var road=FindFirstObjectByType<EdgeView>();
        line.sharedMaterial=road!=null?road.GetComponent<LineRenderer>().sharedMaterial:null;
        line.useWorldSpace=false;
        line.positionCount=40;
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
                var go=new GameObject("Congestion");
                go.transform.SetParent(decorationRoot,false);
                go.transform.position=Vector2.Lerp(a,b,(i+.5f)/count);
                go.transform.localScale=Vector3.one*.16f;
                var dot=go.AddComponent<SpriteRenderer>();
                dot.sprite=Shape(true);
                dot.color=theme.congestion;
                dot.sortingOrder=6;
                congestion.Add((edge,dot));
            }
        }
    }
    private void Route(RouteState state)
    {
        if(!map.IsReady)return;var positions=map.Nodes.ToDictionary(n=>n.Id,n=>n.Position);string current=map.StoreNodeId;int count=0;
        foreach(string id in state.ActiveEdgeIds)
        {
            var edge=map.GetEdge(id);if(!edge.TryTraverse(current,out string next))break;
            while(arrows.Count<=count)
            {
                var go=new GameObject("RouteArrow");
                go.transform.SetParent(decorationRoot,false);
                var text=go.AddComponent<TextMeshPro>();
                text.font=UITheme.Active.font;
                text.alignment=TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta=new Vector2(.5f,.5f);
                go.GetComponent<MeshRenderer>().sortingOrder=12;
                arrows.Add(text);
            }
            var arrow=arrows[count];Vector2 a=positions[current],b=positions[next];var position=Vector2.Lerp(a,b,count%2==0?.4f:.6f);
            if(edge.RoadType==RoadType.Backstreet)
            {
                var direction=(positions[edge.NodeBId]-positions[edge.NodeAId]).normalized;
                position+=new Vector2(-direction.y,direction.x)*.35f;
            }
            arrow.transform.position=position;
            arrow.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            arrow.text=">";
            arrow.fontSize=UITheme.Active.routeArrowSize;
            arrow.color=UITheme.Active.background;
            arrow.fontStyle=FontStyles.Bold;
            count++;
            current=next;
        }
        for(int i=0;i<arrows.Count;i++)arrows[i].text=i<count?">":"";
    }
}
