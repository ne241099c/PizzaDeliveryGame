using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A pizza drawn in layers (dough, sauce, cheese, topping, burn).</summary>
public sealed class PizzaVisual
{
    public RectTransform Root;private Image dough,sauce,cheese,topping,burnt;public RectTransform Blobs;
    public static PizzaVisual Make(Transform parent,string name,float x,float y,float size)
    {
        var v=new PizzaVisual{Root=UiKit.Box(parent,name,x,y,size,size)};
        v.dough=Layer(v.Root,"Dough","pizza_base");
        var mask=UiKit.Stretch(v.Root,"SauceMask",size*.12f,size*.12f,size*.12f,size*.12f);var m=mask.gameObject.AddComponent<Image>();m.sprite=UiKit.Circle;m.raycastTarget=false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;v.Blobs=mask;
        v.sauce=Layer(v.Root,"Sauce","pizza_sauce");v.cheese=Layer(v.Root,"Cheese","pizza_cheese");v.topping=Layer(v.Root,"Topping","pizza_basil");v.burnt=Layer(v.Root,"Burnt","pizza_burnt");
        return v;
    }
    private static Image Layer(RectTransform root,string name,string sprite){var i=UiKit.Stretch(root,name).gameObject.AddComponent<Image>();i.sprite=UiKit.Art(sprite);i.raycastTarget=false;i.preserveAspect=true;return i;}
    /// <summary>Shows a job. For staff assembly the layers appear over time (fraction 0-1).</summary>
    public void Show(PizzaJob job,float staffFraction=1)
    {
        Root.gameObject.SetActive(job!=null);if(job==null)return;
        var stage=job.Stage;bool staffAssembling=job.Worker==PizzaWorker.Staff && stage==PizzaStage.Topping && staffFraction<1;
        int visible=staffAssembling?(staffFraction<.25f?1:staffFraction<.55f?2:4):stage switch{PizzaStage.Ticket=>0,PizzaStage.Dough=>1,PizzaStage.Sauce=>2,PizzaStage.Cheese=>3,_=>4};
        dough.enabled=visible>=1;
        bool hand=job.Worker==PizzaWorker.Player;
        sauce.enabled=visible>=2 && !hand;Blobs.gameObject.SetActive(hand && visible>=1);
        cheese.enabled=visible>=3;
        topping.enabled=visible>=4 && job.AddedTopping.HasValue || staffAssembling && visible>=4;
        var t=job.AddedTopping??job.Order.RequiredTopping;topping.sprite=UiKit.Art(t==Topping.Pepperoni?"pizza_pepperoni":t==Topping.Shrimp?"pizza_shrimp":"pizza_basil");
        bool baked=stage>=PizzaStage.Baked;var warm=new Color(.93f,.80f,.62f);dough.color=baked?warm:Color.white;
        burnt.enabled=job.Burnt;
    }
    public void ClearBlobs(){for(int i=Blobs.childCount-1;i>=0;i--)Object.Destroy(Blobs.GetChild(i).gameObject);}
}

public sealed partial class BusinessView
{
    private sealed class BenchView
    {
        public int Index;public RectTransform Root,Hint,StaffGroup,Empty;public PizzaVisual Pizza;public TextMeshProUGUI Title,HintText,StaffText;public UiBar HintBar,StaffBar;
        public RectTransform StaffAvatar;public PizzaJob Shown;public bool[] Cells=new bool[256];public int Covered;
    }
    private sealed class OvenView
    {
        public int Index;public RectTransform Root,Gauge;public Image Fire;public PizzaVisual Pizza;public TextMeshProUGUI Title,GaugeText;public UiBar GaugeBar;public RectTransform Band;
    }
    private readonly List<BenchView> benches=new();
    private readonly List<OvenView> ovens=new();
    private RectTransform kitchenZone,benchArea,ovenArea,shelf;private TextMeshProUGUI shelfTitle;
    private string shelfSignature="";
    private static readonly (string sprite,string label,DragKind kind,Topping topping)[] Trays=
    {
        ("tray_dough","生地",DragKind.Dough,Topping.Basil),("tray_sauce","ソース",DragKind.Ladle,Topping.Basil),("tray_cheese","チーズ",DragKind.Cheese,Topping.Basil),
        ("tray_basil","バジル",DragKind.Topping,Topping.Basil),("tray_pepperoni","ペパロニ",DragKind.Topping,Topping.Pepperoni),("tray_shrimp","えび",DragKind.Topping,Topping.Shrimp)
    };

    private void BuildKitchen()
    {
        kitchenZone=UiKit.Box(world,"Kitchen",0,0,WorldWidth,1080);
        var floor=UiKit.Stretch(kitchenZone,"Floor").gameObject.AddComponent<Image>();floor.sprite=UiKit.Art("floor_tile");floor.type=Image.Type.Tiled;floor.pixelsPerUnitMultiplier=2;floor.raycastTarget=true;
        // ingredient shelf
        var wood=UiKit.Card(kitchenZone,"Shelf",0,330,1100,190,T.wood,0,8,0);
        UiKit.Fill(UiKit.Box(wood,"Lip",0,166,1100,24),T.woodDark,0);
        for(int i=0;i<Trays.Length;i++)
        {
            var tray=Trays[i];var r=UiKit.Box(wood,"Tray_"+tray.label,40+i*176,14,150,120);
            var img=r.gameObject.AddComponent<Image>();img.sprite=UiKit.Art(tray.sprite);img.preserveAspect=true;
            UiKit.Label(wood,"TrayLabel"+i,tray.label,40+i*176,130,150,30,20,T.paper,FontRole.Black,TextAlignmentOptions.Center);
            var source=r.gameObject.AddComponent<DragSource>();var t=tray;
            source.Payload=()=>cooking.ManualControlEnabled?new DragPayload{Kind=t.kind,Topping=t.topping,Sprite=t.kind==DragKind.Ladle?"ladle":t.sprite,Size=t.kind==DragKind.Ladle?new Vector2(120,120):new Vector2(130,104)}:null;
            source.Clicked=()=>ClickTray(t.kind,t.topping);
        }
        // warming shelf
        shelf=UiKit.Card(kitchenZone,"WarmingShelf",1100,330,820,190,T.plaster,0,8,0);
        for(int i=0;i<2;i++)UiKit.Fill(UiKit.Box(shelf,"Rail"+i,20,60+i*70,780,6),T.steel,3);
        shelfTitle=UiKit.Label(shelf,"Title","保温棚(焼き上がり 0枚)",20,156,780,30,18,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
        benchArea=UiKit.Box(kitchenZone,"Benches",40,560,1070,420);ovenArea=UiKit.Box(kitchenZone,"Ovens",1140,560,760,420);
        BuildCounterStrip();
    }
    private void LayoutStations()
    {
        int n=cooking.BenchCount,m=cooking.OvenCount;if(benches.Count==n && ovens.Count==m)return;
        foreach(var b in benches){ShopHand.Instance?.Unregister(b.Root.GetComponent<DropZone>());Destroy(b.Root.gameObject);Destroy(b.Title.gameObject);}benches.Clear();
        foreach(var o in ovens){ShopHand.Instance?.Unregister(o.Root.GetComponent<DropZone>());Destroy(o.Root.gameObject);Destroy(o.Title.gameObject);Destroy(o.Gauge.gameObject);}ovens.Clear();
        float bw=(1070-(n-1)*24f)/n;for(int i=0;i<n;i++)benches.Add(BuildBench(i,i*(bw+24),bw));
        // Ovens keep a sensible size (max 340 wide) and sit centred in their area.
        float ow=Mathf.Min(340,(760-(m-1)*16f)/m),start=(760-(m*ow+(m-1)*16f))/2f;for(int i=0;i<m;i++)ovens.Add(BuildOven(i,start+i*(ow+16),ow));
    }
    private BenchView BuildBench(int index,float x,float w)
    {
        var v=new BenchView{Index=index};
        v.Root=UiKit.Card(benchArea,"Bench"+index,x,0,w,330,T.steel,18,8,3,null,new Color(T.steelShadow.r,T.steelShadow.g,T.steelShadow.b,.9f));
        var glow=UiKit.Stretch(v.Root,"Glow",-6,-6,-6,-6);var g=UiKit.Fill(glow,T.cheese,24);g.enabled=false;glow.SetAsFirstSibling();
        float size=Mathf.Min(250,w-40);v.Pizza=PizzaVisual.Make(v.Root,"Pizza",(w-size)/2,(330-size)/2+10,size);
        var hit=v.Pizza.Root.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,0);
        var source=v.Pizza.Root.gameObject.AddComponent<DragSource>();int b=index;
        source.Payload=()=>{var j=cooking.BenchJob(b);return j!=null && j.Worker==PizzaWorker.Player && j.Stage==PizzaStage.Topping?new DragPayload{Kind=DragKind.Pizza,Job=j,Sprite="pizza_base",Size=new Vector2(180,180)}:null;};
        source.Clicked=()=>ClickBenchPizza(b);
        v.Empty=UiKit.Box(v.Root,"Empty",w/2-120,110,240,110);UiKit.Dashed(v.Empty,T.steelShadow,12,8,3);
        UiKit.Label(v.Empty,"Text","伝票をここへ",0,0,240,110,22,T.steelShadow,FontRole.Black,TextAlignmentOptions.Center);
        v.Hint=UiKit.Card(v.Root,"Hint",16,16,Mathf.Min(300,w-32),96,T.paper,12,4,3);
        v.HintText=UiKit.Label(v.Hint,"Text","",14,8,270,32,19,T.line,FontRole.Black);
        v.HintBar=UiKit.Bar(v.Hint,"Bar",14,56,Mathf.Min(270,w-60),22,T.cheese);
        v.StaffGroup=UiKit.Box(v.Root,"Staff",0,0,w,330);
        v.StaffAvatar=UiKit.Avatar(v.StaffGroup,"Avatar",14,14,86,T.info,"M");
        var staffCard=UiKit.Card(v.StaffGroup,"Caption",20,236,w-40,74,T.paper,12,4,3);
        UiKit.Badge(staffCard,"Kitchen",ShopStrings.Kitchen,12,10,T.info,Color.white,28,15);
        v.StaffText=UiKit.Label(staffCard,"Text","",84,4,w-140,36,18,T.line,FontRole.Black);v.StaffBar=UiKit.Bar(staffCard,"Bar",84,42,w-140,20,T.info);
        v.Title=UiKit.Label(benchArea,"BenchTitle"+index,"",x,342,w,34,20,T.line,FontRole.Black,TextAlignmentOptions.Center);
        var zone=v.Root.gameObject.AddComponent<DropZone>();zone.Glow=g;zone.Accepts=p=>BenchAccepts(b,p);zone.Dropped=p=>BenchDrop(b,p);zone.Hovering=(p,pos)=>Trace(v,p,pos);
        var rootHit=v.Root.gameObject.AddComponent<Image>();rootHit.color=new Color(0,0,0,0);
        ShopHand.Instance?.Register(zone);return v;
    }
    private OvenView BuildOven(int index,float x,float w)
    {
        var v=new OvenView{Index=index};float h=w*220f/300f;
        v.Root=UiKit.Box(ovenArea,"Oven"+index,x,0,w,h);
        var glow=UiKit.Stretch(v.Root,"Glow",-8,-8,-8,-8);var g=UiKit.Fill(glow,T.cheese,28);g.enabled=false;
        var body=UiKit.Stretch(v.Root,"Body").gameObject.AddComponent<Image>();body.sprite=UiKit.Art("oven");body.raycastTarget=true;
        var mouth=UiKit.Stretch(v.Root,"Mouth",w*26/300,h*24/220,w*26/300,h*46/220);v.Fire=mouth.gameObject.AddComponent<Image>();v.Fire.sprite=UiKit.Art("oven_fire");v.Fire.raycastTarget=false;
        float size=Mathf.Min(h*.62f,w*.5f);v.Pizza=PizzaVisual.Make(v.Root,"Pizza",(w-size)/2,h*.16f,size);
        v.Title=UiKit.Label(ovenArea,"OvenTitle"+index,"",x,h+76,w,34,20,T.line,FontRole.Black,TextAlignmentOptions.Center);
        v.Gauge=UiKit.Card(ovenArea,"Gauge"+index,x,h+12,w,58,T.paper,12,4,3);
        v.GaugeText=UiKit.Label(v.Gauge,"Text","焼き具合",12,2,w-24,26,16,T.line,FontRole.Black);
        v.GaugeBar=UiKit.Bar(v.Gauge,"Bar",12,30,w-24,18,T.cheese);
        v.Band=UiKit.Box(v.GaugeBar.transform,"Band",0,0,10,18);UiKit.Fill(v.Band,T.basil,4);
        var zone=v.Root.gameObject.AddComponent<DropZone>();zone.Glow=g;int o=index;
        zone.Accepts=p=>p.Kind==DragKind.Pizza && p.Job!=null && p.Job.Stage==PizzaStage.Topping && p.Job.Worker==PizzaWorker.Player && cooking.OvenJob(o)==null;
        zone.Dropped=p=>{if(!cooking.PutInOven(p.Job.Bench,o))Toast("窯に入れられませんでした。",T.berry);};
        var click=v.Root.gameObject.AddComponent<DragSource>();click.Clicked=()=>ClickOven(o);
        ShopHand.Instance?.Register(zone);return v;
    }

    private bool BenchAccepts(int bench,DragPayload p)
    {
        var job=cooking.BenchJob(bench);
        return p.Kind switch
        {
            DragKind.Ticket=>cooking.ClaimRejection(p.Order,bench).Length==0,
            DragKind.Dough=>job!=null && job.Worker==PizzaWorker.Player && job.Stage==PizzaStage.Ticket,
            DragKind.Ladle=>job!=null && job.Worker==PizzaWorker.Player && job.Stage==PizzaStage.Dough,
            DragKind.Cheese=>job!=null && job.Worker==PizzaWorker.Player && job.Stage==PizzaStage.Sauce,
            DragKind.Topping=>job!=null && job.Worker==PizzaWorker.Player && job.Stage==PizzaStage.Cheese,
            _=>false
        };
    }
    private void BenchDrop(int bench,DragPayload p)
    {
        switch(p.Kind)
        {
            case DragKind.Ticket:cooking.Claim(p.Order,bench);break;
            case DragKind.Dough:cooking.AddDough(bench);break;
            case DragKind.Ladle:{var v=benches[bench];if(v.Covered>0)cooking.AddSauce(bench,Coverage(v));break;}
            case DragKind.Cheese:cooking.AddCheese(bench);break;
            case DragKind.Topping:cooking.AddTopping(bench,p.Topping);break;
        }
    }
    // Sauce coverage: 16x16 cells inside the pizza circle; the ladle paints a disc around the pointer.
    private static float Coverage(BenchView v){int total=0,hit=0;for(int y=0;y<16;y++)for(int x=0;x<16;x++){if(!Inside(x,y))continue;total++;if(v.Cells[y*16+x])hit++;}return total==0?0:(float)hit/total;}
    private static bool Inside(int x,int y){float dx=x-7.5f,dy=y-7.5f;return dx*dx+dy*dy<=7.6f*7.6f;}
    private void Trace(BenchView v,DragPayload p,Vector2 screen)
    {
        if(p.Kind!=DragKind.Ladle)return;var blobs=v.Pizza.Blobs;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(blobs,screen,null,out var local))return;
        var rect=blobs.rect;float u=(local.x-rect.xMin)/rect.width,w=(local.y-rect.yMin)/rect.height;if(u<0||u>1||w<0||w>1)return;
        int cx=Mathf.FloorToInt(u*16),cy=Mathf.FloorToInt(w*16);bool added=false;
        for(int y=cy-2;y<=cy+2;y++)for(int x=cx-2;x<=cx+2;x++)
        {
            if(x<0||y<0||x>15||y>15||!Inside(x,y))continue;float dx=x-cx,dy=y-cy;if(dx*dx+dy*dy>5)continue;
            if(!v.Cells[y*16+x]){v.Cells[y*16+x]=true;v.Covered++;added=true;}
        }
        if(added){var blob=UiKit.Box(blobs,"Blob",local.x-rect.xMin-rect.width*.13f,rect.yMax-local.y-rect.height*.13f,rect.width*.26f,rect.height*.26f);var i=blob.gameObject.AddComponent<Image>();i.sprite=UiKit.Art("sauce_blob");i.raycastTarget=false;}
    }
    /// <summary>Trace the ladle at a point on bench pizza (0-1 in each axis); used by the hand and by validation.</summary>
    public float TraceSauce(int bench,Vector2 uv)
    {
        if(bench<0 || bench>=benches.Count)return 0;var v=benches[bench];var job=cooking.BenchJob(bench);
        if(job!=v.Shown){v.Shown=job;v.Pizza.ClearBlobs();System.Array.Clear(v.Cells,0,v.Cells.Length);v.Covered=0;v.Pizza.Show(job);}
        var rect=v.Pizza.Blobs.rect;
        var screen=RectTransformUtility.WorldToScreenPoint(null,v.Pizza.Blobs.TransformPoint(new Vector3(rect.xMin+uv.x*rect.width,rect.yMin+uv.y*rect.height,0)));
        Trace(v,new DragPayload{Kind=DragKind.Ladle},screen);return Coverage(v);
    }
    public bool CommitSauce(int bench){if(bench<0 || bench>=benches.Count || benches[bench].Covered==0)return false;return cooking.AddSauce(bench,Coverage(benches[bench]));}
    public void ClickTrayFor(DragKind kind,Topping topping)=>ClickTray(kind,topping);
    public void ClickPizzaOn(int bench)=>ClickBenchPizza(bench);
    public void ClickOvenAt(int oven)=>ClickOven(oven);
    private void ClickTray(DragKind kind,Topping topping)
    {
        if(!cooking.ManualControlEnabled)return;
        PizzaStage need=kind switch{DragKind.Dough=>PizzaStage.Ticket,DragKind.Cheese=>PizzaStage.Sauce,DragKind.Topping=>PizzaStage.Cheese,_=>PizzaStage.Dough};
        int bench=Enumerable.Range(0,cooking.BenchCount).FirstOrDefault(b=>cooking.BenchJob(b) is PizzaJob j && j.Worker==PizzaWorker.Player && j.Stage==need);
        var job=cooking.BenchJob(bench);
        if(job==null || job.Worker!=PizzaWorker.Player || job.Stage!=need){Toast(kind==DragKind.Ladle?"おたまをつかんで、生地の上をなぞりましょう。":"先に伝票を作業台へ置きましょう。",T.cheese,null,"icon_hand");return;}
        if(kind==DragKind.Ladle){Toast("おたまをつかんで、生地の上をなぞりましょう。",T.cheese,null,"icon_hand");return;}
        if(kind==DragKind.Dough)cooking.AddDough(bench);else if(kind==DragKind.Cheese)cooking.AddCheese(bench);else cooking.AddTopping(bench,topping);
    }
    private void ClickBenchPizza(int bench)
    {
        var job=cooking.BenchJob(bench);if(job==null || job.Worker!=PizzaWorker.Player || job.Stage!=PizzaStage.Topping)return;
        for(int o=0;o<cooking.OvenCount;o++)if(cooking.OvenJob(o)==null){cooking.PutInOven(bench,o);return;}
        Toast("あいている窯がありません。",T.berry,null,"icon_flame");
    }
    private void ClickOven(int oven)
    {
        if(cooking.CanTakeOut(oven)){cooking.TakeOut(oven);return;}
        var job=cooking.OvenJob(oven);if(job!=null && job.Worker==PizzaWorker.Player)Toast("まだ生焼けです。ゲージが緑の帯に入ったら取り出しましょう。",T.cheese,null,"icon_flame");
    }

    private void RefreshKitchen()
    {
        LayoutStations();float now=clock.ElapsedSeconds;
        foreach(var v in benches)
        {
            var job=cooking.BenchJob(v.Index);
            if(job!=v.Shown){v.Shown=job;v.Pizza.ClearBlobs();System.Array.Clear(v.Cells,0,v.Cells.Length);v.Covered=0;}
            bool player=job!=null && job.Worker==PizzaWorker.Player,staff=job!=null && job.Worker==PizzaWorker.Staff;
            float fraction=staff?Mathf.Clamp01((cooking.Progress(job.Order)-.05f)/.45f):1;
            v.Pizza.Show(job,fraction);v.Empty.gameObject.SetActive(job==null);
            v.Hint.gameObject.SetActive(player);v.StaffGroup.gameObject.SetActive(staff);
            string owner=player?"あなた":staff?(job.Staff!=null?job.Staff.Name+"(内勤)":"内勤"):"あいている";
            v.Title.text=$"作業台{v.Index+1} ・ {owner}";
            if(player)
            {
                switch(job.Stage)
                {
                    case PizzaStage.Ticket:v.HintText.text="① 生地を置く";v.HintBar.Set(0);break;
                    case PizzaStage.Dough:{float c=Coverage(v);v.HintText.text=$"② ソースをなぞる {c*100:F0}% / 70%で★";v.HintBar.Set(c,cooking.SauceThreshold,c>=cooking.SauceThreshold?T.basil:T.cheese);break;}
                    case PizzaStage.Sauce:v.HintText.text="③ チーズをのせる";v.HintBar.Set(.5f,-1,T.cheese);break;
                    case PizzaStage.Cheese:v.HintText.text="④ 具をのせる: "+ShopStrings.Topping(job.Order.RequiredTopping);v.HintBar.Set(.7f,-1,T.cheese);break;
                    default:v.HintText.text="⑤ 窯へ入れる";v.HintBar.Set(1,-1,T.basil);break;
                }
            }
            if(staff)
            {
                UiKit.SetAvatar(v.StaffAvatar,T.info,job.Staff!=null?job.Staff.Name.Substring(0,1):"?");
                bool waiting=fraction>=1;v.StaffText.text=waiting?"窯があくのを待っている":fraction<.25f?"生地を伸ばしている":fraction<.55f?"ソースを塗っている":"チーズと具をのせている";
                v.StaffBar.Set(fraction);
            }
        }
        foreach(var v in ovens)
        {
            var job=cooking.OvenJob(v.Index);v.Pizza.Show(job);
            float elapsed=job!=null?now-job.OvenStartedAt:0;
            v.Fire.color=job!=null?new Color(1,1,1,.75f+.25f*Mathf.Sin(Time.unscaledTime*6)):new Color(1,1,1,.25f);
            bool player=job!=null && job.Worker==PizzaWorker.Player;v.Gauge.gameObject.SetActive(job!=null);
            v.Title.text=$"窯{v.Index+1} ・ "+(job==null?"あいている":player?"焼いている":(job.Staff!=null?job.Staff.Name:"内勤")+"が見ている");
            if(job!=null)
            {
                float range=cooking.BurnAt;float progress=Mathf.Clamp01(elapsed/range);
                bool burnt=elapsed>=cooking.BurnAt,inBand=elapsed>=cooking.BandStart && elapsed<=cooking.BandEnd,over=elapsed>cooking.BandEnd;
                v.GaugeBar.Set(progress,-1,burnt?T.line:over?T.berry:inBand?T.basil:T.cheese);
                float w=((RectTransform)v.GaugeBar.transform).sizeDelta.x;v.Band.anchoredPosition=new Vector2(cooking.BandStart/range*w,0);v.Band.sizeDelta=new Vector2((cooking.BandEnd-cooking.BandStart)/range*w,18);
                v.GaugeText.text=!player?"焼き具合 (自動で取り出し)":burnt?"焦げた!":over?"焦げそう!":inBand?"いま! 取り出す":"焼き具合 あと少し";
                v.GaugeText.color=inBand && player?T.basil:over && player?T.berry:T.line;
            }
        }
        // The shelf stocks packed boxes (and any baked pizza waiting for a free box slot).
        var boxes=handover.RackOrders.ToList();var unboxed=cooking.Shelf.Concat(cooking.Jobs.Where(j=>j.Stage==PizzaStage.Boxed)).ToList();
        string sig=string.Join(",",boxes.Select(o=>o.Id))+"|"+string.Join(",",unboxed.Select(j=>j.Order.Id+j.Stage));
        if(sig!=shelfSignature){shelfSignature=sig;RebuildShelf(boxes,unboxed);}
        shelfTitle.text=boxes.Count==0?"梱包済みのピザがここに並びます":$"梱包済み {boxes.Count} / {cooking.RackCapacity} ・ 店にいる配達員へドラッグして積む";
        foreach(var item in shelfItems)
        {
            var order=boxes.FirstOrDefault(o=>item.name=="Box_"+Number(o));if(order==null)continue;
            float fraction=Mathf.Clamp01((order.Deadline-clock.ElapsedSeconds)/Mathf.Max(.01f,order.TimeLimit));
            var bar=item.Find("Deadline")?.GetComponent<UiBar>();if(bar!=null)bar.Set(fraction,-1,T.DeadlineColor(fraction));
        }
        RefreshCounterStrip();
    }
    private readonly List<RectTransform> shelfItems=new();
    private void RebuildShelf(List<Order> boxes,List<PizzaJob> waiting)
    {
        foreach(var item in shelfItems)Destroy(item.gameObject);shelfItems.Clear();
        int shown=0;
        foreach(var order in boxes.Take(cooking.RackCapacity))
        {
            var slot=UiKit.Box(shelf,"Box_"+Number(order),20+shown*130,6,118,144);shelfItems.Add(slot);shown++;
            var img=slot.gameObject.AddComponent<Image>();img.sprite=UiKit.Art("box_closed");img.preserveAspect=true;img.raycastTarget=true;
            var number=UiKit.Label(slot,"Number","#"+Number(order),0,44,118,34,24,T.line,FontRole.Display,TextAlignmentOptions.Center);number.verticalAlignment=VerticalAlignmentOptions.Geometry;
            UiKit.Label(slot,"Stars",new string('★',Mathf.Max(1,order.Stars)),0,84,118,24,18,T.cheese,FontRole.Black,TextAlignmentOptions.Center);
            UiKit.Bar(slot,"Deadline",10,120,98,14,T.basil);
            var source=slot.gameObject.AddComponent<DragSource>();var o=order;
            source.Payload=()=>handover.RackOrders.Contains(o)?new DragPayload{Kind=DragKind.Box,Order=o,Sprite="box_closed",Size=new Vector2(110,110)}:null;
            source.Clicked=()=>QuickBag(o);
        }
        foreach(var job in waiting)
        {
            if(shown>=6)break;var slot=UiKit.Box(shelf,"Waiting_"+Number(job.Order),20+shown*130,6,118,144);shelfItems.Add(slot);shown++;
            var pizza=PizzaVisual.Make(slot,"Pizza",4,4,110);pizza.Show(job);
            UiKit.Badge(slot,"Wait","空き待ち",8,116,T.cheese,T.line,28,16);
        }
    }

    // ---- couriers waiting at the counter (drop packed boxes on them) ----
    private RectTransform counterStrip;private string counterStripSignature="";private readonly List<RectTransform> counterTokens=new();
    private void BuildCounterStrip()
    {
        counterStrip=UiKit.Card(kitchenZone,"CounterStrip",1110,938,790,128,T.wood,16,6,3);
        UiKit.Label(counterStrip,"Title","受け渡し",14,4,200,28,16,T.paper,FontRole.Black);
    }
    private void RefreshCounterStrip()
    {
        if(counterStrip==null)return;
        string sig=string.Join(",",roster.People.Select(p=>UiKit.Ref(p)+p.Status+string.Join("/",handover.BagOf(p).Select(o=>o.Id))+p.Capacity+(roster.SelectedPerson==p)));
        if(sig!=counterStripSignature)
        {
            counterStripSignature=sig;
            foreach(var t in counterTokens){var z=t.GetComponent<DropZone>();if(z!=null)ShopHand.Instance?.Unregister(z);Destroy(t.gameObject);}counterTokens.Clear();
            int n=Mathf.Max(1,roster.People.Count);float w=Mathf.Min(256,(762f-(n-1)*8)/n);
            for(int i=0;i<roster.People.Count;i++)counterTokens.Add(CourierToken(roster.People[i],14+i*(w+8),30,w,90));
        }
        foreach(var t in counterTokens)
        {
            var person=roster.People.FirstOrDefault(p=>t.name=="Token_"+p.Name);var state=t.Find("State")?.GetComponent<TextMeshProUGUI>();
            if(person!=null && state!=null)state.text=person.IsAvailable?(handover.BagOf(person).Count>0?"積み込み中":"待機"):person.Status==DeliveryStatus.Training?"訓練中":$"あと{Mathf.Max(0,(person.CurrentTrip?.ReturnsAt??0)-clock.ElapsedSeconds):F0}秒";
        }
    }
    private RectTransform CourierToken(DeliveryPerson person,float x,float y,float w,float h)
    {
        bool here=person.IsAvailable;var bag=handover.BagOf(person);
        var token=UiKit.Box(counterStrip,"Token_"+person.Name,x,y,w,h);
        var glow=UiKit.Stretch(token,"Glow",-5,-5,-5,-5);var g=UiKit.Fill(glow,T.cheese,18);g.enabled=false;
        // Dark wood token so the paper-coloured text stays readable; the selected courier gets a cheese edge.
        if(roster.SelectedPerson==person)UiKit.Fill(UiKit.Stretch(token,"Selected",-3,-3,-3,-3),T.cheese,17);
        var face=UiKit.Fill(UiKit.Stretch(token,"Face"),new Color(T.woodDark.r,T.woodDark.g,T.woodDark.b,.9f),14,true);
        float size=Mathf.Min(64,h-12);UiKit.Avatar(token,"Avatar",6,(h-size)/2,size,here?T.CourierColor(person.Name):T.disabled,person.Name.Substring(0,1));
        UiKit.Label(token,"Name",person.Name,size+12,4,w-size-16,30,20,T.paper,FontRole.Display);
        UiKit.Label(token,"State",here?"待機":ShopStrings.Status(person.Status),size+12,32,w-size-16,24,16,T.paper,FontRole.Black);
        float slotW=Mathf.Min(34,(w-size-18)/Mathf.Max(1,person.Capacity)-4);
        for(int s=0;s<person.Capacity;s++)
        {
            var slot=UiKit.Box(token,"Bag"+s,size+12+s*(slotW+4),58,slotW,28);
            if(s<bag.Count){var img=slot.gameObject.AddComponent<Image>();img.sprite=UiKit.Art("box_closed");img.preserveAspect=true;img.raycastTarget=false;}
            else UiKit.Dashed(slot,new Color(1,1,1,.75f),6,4,2);
        }
        var zone=token.gameObject.AddComponent<DropZone>();zone.Glow=g;var p=person;
        zone.Accepts=pl=>pl.Kind==DragKind.Box && handover.BagRejection(pl.Order,p).Length==0;
        zone.Dropped=pl=>{if(!handover.Bag(pl.Order,p))Toast(handover.LastMessage,T.berry,null,"icon_box");};
        ShopHand.Instance?.Register(zone);
        var click=token.gameObject.AddComponent<DragSource>();click.Clicked=()=>ClickCourier(p);
        if(!here)UiKit.Group(token).alpha=.55f;
        return token;
    }
}
