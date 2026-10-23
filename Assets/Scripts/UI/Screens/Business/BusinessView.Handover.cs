using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class BusinessView
{
    [Header("Handover")]
    [SerializeField] private RectTransform packing,rackArea,counter;
    [SerializeField] private TextMeshProUGUI packingTitle,rackTitle;
    [SerializeField] private DropZone packingZone;
    private string packingSignature="",rackSignature="",counterSignature="";
    private readonly List<RectTransform> packingItems=new(),rackItems=new(),counterItems=new();
    private readonly List<(Order order,TextMeshProUGUI number)> rackNumbers=new();
    private readonly List<(DeliveryPerson person,TextMeshProUGUI state)> counterStates=new();

    private void BuildHandover()
    {
        // packing table (left of the handover area)
        packing=UiKit.Card(world,"PackingTable",1960,560,440,330,T.wood,18,8,3);
        UiKit.Fill(UiKit.Box(packing,"Top",14,14,412,302),new Color(1,1,1,.12f),12);
        UiKit.Picture(packing,"BoxStack","box_stack",18,170,150,120);
        packingTitle=UiKit.Label(world,"PackingTitle","梱包台",1960,902,440,34,20,T.line,FontRole.Black,TextAlignmentOptions.Center);
        var glow=UiKit.Stretch(packing,"Glow",-6,-6,-6,-6);var g=UiKit.Fill(glow,T.cheese,24);g.enabled=false;glow.SetAsFirstSibling();
        packingZone=packing.gameObject.AddComponent<DropZone>();
        packingZone.Glow=g;
        var hit=packing.gameObject.AddComponent<Image>();
        hit.color=new Color(0,0,0,0);
        // counter rack for closed boxes (top) and the courier counter (right)
        rackArea=UiKit.Card(world,"Rack",1960,330,900,190,T.plaster,0,8,0);
        for(int i=0;i<cooking.RackCapacity;i++)
        {
            var spot=UiKit.Box(rackArea,"Spot"+i,24+i*146,14,128,128);
            UiKit.Dashed(spot,new Color(T.inkSub.r,T.inkSub.g,T.inkSub.b,.5f),10,8,2);
        }
        rackTitle=UiKit.Label(rackArea,"Title","受け渡しの台 0 / 6",20,152,860,30,18,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
        counter=UiKit.Card(world,"Counter",2420,560,440,380,T.wood,18,8,3);
        UiKit.Label(world,"CounterTitle","受け渡しカウンター",2420,948,440,34,20,T.line,FontRole.Black,TextAlignmentOptions.Center);
    }

    private void BindHandover()
    {
        packingZone.Accepts=p=>p.Kind==DragKind.Pizza && p.Job!=null && p.Job.Stage==PizzaStage.Baked && p.Job.Worker==PizzaWorker.Player && cooking.PackingTable.Count()<cooking.PackingSlots;
        packingZone.Dropped=p=>cooking.Box(p.Job);
    }
    private void RefreshHandover()
    {
        // open boxes on the packing table
        var open=cooking.PackingTable.ToList();
        string sig=string.Join(",",open.Select(j=>j.Order.Id));
        if(sig!=packingSignature)
        {
            packingSignature=sig;foreach(var i in packingItems)Destroy(i.gameObject);packingItems.Clear();
            for(int k=0;k<open.Count;k++)
            {
                var job=open[k];var slot=UiKit.Box(packing,"OpenBox_"+Number(job.Order),180+k*126,40,120,120);packingItems.Add(slot);
                UiKit.Picture(slot,"Box","box_open",0,0,120,120);var pizza=PizzaVisual.Make(slot,"Pizza",14,14,92);pizza.Show(job);
                UiKit.Badge(slot,"Number","#"+Number(job.Order),0,0,T.paper,T.line,26,14);
                var close=UiKit.Button(slot,"Close","閉じる",StickerKind.Primary,0,126,120,52,20,null);var j=job;
                close.Button.onClick.AddListener(()=>
                {
                    string reason=cooking.CloseRejection(j);
                    if(reason.Length>0)Toast(reason,T.berry,null,"icon_box");
                    else cooking.CloseBox(j);
                });
            }
        }
        packingTitle.text=open.Count==0?"梱包台 ・ 焼けたピザをここへ":"梱包台 ・ 箱を閉じると伝票が貼られます";
        // packed boxes waiting on the rack
        var rack=handover.RackOrders.ToList();
        sig=string.Join(",",rack.Select(o=>o.Id));
        if(sig!=rackSignature)
        {
            rackSignature=sig;foreach(var i in rackItems)Destroy(i.gameObject);rackItems.Clear();rackNumbers.Clear();
            for(int k=0;k<rack.Count && k<cooking.RackCapacity;k++)
            {
                var order=rack[k];var slot=UiKit.Box(rackArea,"Box_"+Number(order),24+k*146,14,128,128);rackItems.Add(slot);
                var img=slot.gameObject.AddComponent<Image>();img.sprite=UiKit.Art("box_closed");img.preserveAspect=true;
                rackNumbers.Add((order,UiKit.Label(slot,"Number","#"+Number(order),0,52,128,30,22,T.line,FontRole.Black,TextAlignmentOptions.Center)));
                UiKit.Label(slot,"Stars",new string('★',Mathf.Max(1,order.Stars)),0,96,128,26,18,T.cheese,FontRole.Black,TextAlignmentOptions.Center);
                var source=slot.gameObject.AddComponent<DragSource>();var o=order;
                source.Payload=()=>new DragPayload{Kind=DragKind.Box,Order=o,Sprite="box_closed",Size=new Vector2(110,110)};
                source.Clicked=()=>QuickBag(o);
            }
        }
        rackTitle.text=$"受け渡しの台 {rack.Count} / {cooking.RackCapacity}";
        foreach(var (order,number) in rackNumbers)
        {
            float fraction=Mathf.Clamp01((order.Deadline-clock.ElapsedSeconds)/Mathf.Max(.01f,order.TimeLimit));
            number.color=fraction<=T.deadlineUrgent?T.berry:T.line;
        }
        // couriers at the counter
        sig=string.Join(",",roster.People.Select(p=>UiKit.Ref(p)+p.Status+handover.BagOf(p).Count+p.Capacity+(roster.SelectedPerson==p)));
        if(sig!=counterSignature){counterSignature=sig;RebuildCounter();}
        foreach(var (person,state) in counterStates)
        {
            if(!person.IsAvailable)state.text=person.Status==DeliveryStatus.Training?"訓練中":$"{ShopStrings.Status(person.Status)} あと{Mathf.Max(0,(person.CurrentTrip?.ReturnsAt??0)-clock.ElapsedSeconds):F0}秒";
        }
    }
    private void RebuildCounter()
    {
        foreach(var i in counterItems)
        {
            var z=i.GetComponent<DropZone>();
            if(z!=null)ShopHand.Instance?.Unregister(z);
            Destroy(i.gameObject);
        }
        counterItems.Clear();counterStates.Clear();
        int count=roster.People.Count;float rowH=Mathf.Min(120,350f/Mathf.Max(1,count));
        for(int k=0;k<count;k++)
        {
            var person=roster.People[k];var row=UiKit.Box(counter,"Courier_"+person.Name,14,14+k*rowH,412,rowH-8);counterItems.Add(row);
            bool here=person.IsAvailable;var bag=handover.BagOf(person);
            var face=UiKit.Stretch(row,"Face");var f=UiKit.Fill(face,new Color(1,1,1,roster.SelectedPerson==person?.35f:.15f),14,true);
            var glow=UiKit.Stretch(row,"Glow",-4,-4,-4,-4);var g=UiKit.Fill(glow,T.cheese,16);g.enabled=false;glow.SetAsFirstSibling();
            float size=Mathf.Min(84,rowH-20);
            var avatar=UiKit.Avatar(row,"Avatar",8,(rowH-8-size)/2,size,here?T.CourierColor(person.Name):T.disabled,person.Name.Substring(0,1));
            UiKit.Label(row,"Name",person.Name,size+20,4,160,34,24,T.paper,FontRole.Display);
            counterStates.Add((person,UiKit.Label(row,"State",here?"待機":ShopStrings.Status(person.Status),size+20,36,200,28,16,T.paper,FontRole.Black)));
            for(int s=0;s<person.Capacity;s++)
            {
                var slot=UiKit.Box(row,"Bag"+s,250+s*76,(rowH-8-60)/2,68,60);
                if(s<bag.Count)
                {
                    var img=slot.gameObject.AddComponent<Image>();
                    img.sprite=UiKit.Art("box_closed");
                    img.preserveAspect=true;
                    UiKit.Label(slot,"N","#"+Number(bag[s]),0,16,68,28,16,T.line,FontRole.Black,TextAlignmentOptions.Center);
                }
                else UiKit.Dashed(slot,new Color(1,1,1,.7f),8,6,2);
            }
            var zone=row.gameObject.AddComponent<DropZone>();zone.Glow=g;var p=person;
            zone.Accepts=pl=>pl.Kind==DragKind.Box && handover.BagRejection(pl.Order,p).Length==0;
            zone.Dropped=pl=>handover.Bag(pl.Order,p);ShopHand.Instance?.Register(zone);
            var click=row.gameObject.AddComponent<DragSource>();click.Clicked=()=>ClickCourier(p);
            if(!here){var cg=UiKit.Group(row);cg.alpha=.6f;}
        }
    }
    private void QuickBag(Order order)
    {
        var target=roster.SelectedPerson!=null && handover.BagRejection(order,roster.SelectedPerson).Length==0?roster.SelectedPerson:roster.People.FirstOrDefault(p=>handover.BagRejection(order,p).Length==0);
        if(target==null){Toast("箱を受け取れる配達員がいません。",T.berry,null,"icon_bike");return;}
        handover.Bag(order,target);
    }
    private void ClickCourier(DeliveryPerson person)
    {
        if(!person.IsAvailable){Toast($"{person.Name} は{ShopStrings.Status(person.Status)}です。",T.cheese,null,"icon_bike");return;}
        handover.Select(person);if(handover.BagOf(person).Count>0)ShowZone(ShopZone.Dispatch);
    }
}
