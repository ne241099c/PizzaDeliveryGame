using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>The three tutorial chapters: 1 作る (cooking by hand), 2 配達する (lanes, route, departure),
/// 3 内勤と作り直し (kitchen staff, a failed pizza, discarding and remaking).</summary>
public sealed partial class TutorialDirector
{
    private const float TutorialPromise=900;
    private OrderBook Orders=>FindFirstObjectByType<OrderBook>();
    private CookingManager Cooking=>FindFirstObjectByType<CookingManager>();
    private CourierRoster Roster=>FindFirstObjectByType<CourierRoster>();
    private HandoverManager Handover=>FindFirstObjectByType<HandoverManager>();

    /// <summary>Opens the shop with no random orders (the tutorial adds its own).</summary>
    private void OpenShop(ShopZone zone)
    {
        var spawner=FindFirstObjectByType<OrderSpawner>();if(spawner!=null)spawner.enabled=false;
        campaign.StartBusiness();clock.AutomaticTick=false;
        router.Show(ScreenId.Business,true);business.ShowZone(zone,true);
    }
    /// <summary>Delivery addresses nearest to the shop first, so the tutorial trips are short.</summary>
    private string[] NearestHomes(int count)
    {
        var map=FindFirstObjectByType<MapManager>();var spawner=FindFirstObjectByType<OrderSpawner>();
        return map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore)
            .OrderBy(n=>{try{return spawner.ShortestArrivalSeconds(n.Id);}catch(System.InvalidOperationException){return float.MaxValue;}})
            .Take(count).Select(n=>n.Id).ToArray();
    }
    private Order AddOrder(string home,BakingState baking=BakingState.Waiting,float promise=TutorialPromise)
    {
        var order=new Order(home,"Delivery to "+home,clock.ElapsedSeconds,promise,baking);
        Orders.AddOrder(order);return order;
    }
    private PizzaStage StageOf(Order o)=>Cooking.JobFor(o) is PizzaJob j?j.Stage:o.IsReady?PizzaStage.Packed:PizzaStage.Ticket;
    private RectTransform Ticket()=>business.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name.StartsWith("Ticket_") && t.gameObject.activeInHierarchy);
    /// <summary>The card of a baked order that is not in a lane yet (the one to drag next).</summary>
    private RectTransform OrderCard()
    {
        var next=Orders.Orders.FirstOrDefault(o=>o.IsReady && o.Status==OrderStatus.Pending && !Handover.IsBagged(o));
        return next!=null?Part("Order_"+business.Number(next)):null;
    }
    /// <summary>Two homes a fair way from the shop and near each other, so the joint route is easy to see.</summary>
    private string[] DeliveryHomes()
    {
        var map=FindFirstObjectByType<MapManager>();var spawner=FindFirstObjectByType<OrderSpawner>();
        var homes=map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore)
            .Select(n=>{float t;try{t=spawner.ShortestArrivalSeconds(n.Id);}catch(System.InvalidOperationException){t=float.MaxValue;}return (node:n,time:t);})
            .Where(h=>h.time<float.MaxValue).OrderBy(h=>h.time).ToList();
        if(homes.Count<2)return homes.Select(h=>h.node.Id).DefaultIfEmpty(NearestHomes(1)[0]).ToArray();
        var first=homes[Mathf.Clamp(homes.Count*2/5,0,homes.Count-1)];
        var second=homes.Where(h=>h.node!=first.node && Mathf.Abs(h.time-first.time)<first.time*.5f)
            .OrderBy(h=>Vector2.Distance(h.node.Position,first.node.Position)).Select(h=>(h.node,h.time)).DefaultIfEmpty(homes.First(h=>h.node!=first.node)).First();
        return new[]{first.node.Id,second.node.Id};
    }
    private static string TrayName(Topping t)=>"Tray_"+ShopStrings.Topping(t);

    // ---- 1. 作る ----
    private void ChapterCooking()
    {
        // The player cooks alone in this chapter.
        FindFirstObjectByType<KitchenCrew>().Restore(new List<KitchenStaff>());
        OpenShop(ShopZone.Kitchen);
        var o=AddOrder(NearestHomes(1)[0]);
        Explain("ようこそ！ ここは厨房です。注文が入ると、上のレールに伝票が届きます。",()=>new[]{Ticket()});
        Add("伝票を、下の作業台へドラッグしましょう。",()=>Cooking.JobFor(o)!=null,()=>new[]{Ticket(),Part("Bench0")});
        Add("生地を、作業台へドラッグします。",()=>StageOf(o)>=PizzaStage.Dough,()=>Parts("Tray_生地","Bench0"));
        Add("ソースを生地へドラッグして、なぞるように塗り広げます。たっぷり塗ると★が増えます。",()=>StageOf(o)>=PizzaStage.Sauce,()=>Parts("Tray_ソース","Bench0"));
        Add("チーズをのせましょう。",()=>StageOf(o)>=PizzaStage.Cheese,()=>Parts("Tray_チーズ","Bench0"));
        Add($"伝票に書かれた具「{o.Ingredients}」をのせます。",()=>StageOf(o)>=PizzaStage.Topping,()=>Parts(TrayName(o.RequiredTopping),"Bench0"));
        Add("できたピザを、窯へドラッグします。",()=>StageOf(o)>=PizzaStage.Baking || Cooking.JobFor(o)?.QueuedForOven==true,()=>Parts("Bench0","Oven0"));
        Add("焼き具合のゲージが緑の帯に入ったら、窯をクリックして取り出しましょう。",()=>StageOf(o)>=PizzaStage.Baked,()=>Parts("Gauge0","Oven0"));
        Add("焼けたピザは自動で箱に入り、保温棚に並びます。失敗したときは「そのまま出す」か、ゴミ箱で作り直しを選びます。",()=>o.IsReady,()=>Parts("WarmingShelf"));
        Explain("完成！ ★の数で品質ボーナスが変わります。できたピザは配達指示の画面で届けます（2章）。",()=>Parts("WarmingShelf"));
    }

    // ---- 2. 配達する ----
    private DeliveryPerson Courier=>Roster.People.OrderByDescending(p=>Handover.BagOf(p).Count).ThenByDescending(p=>p.Capacity).First();
    private void ChapterDelivery()
    {
        OpenShop(ShopZone.Dispatch);
        var homes=DeliveryHomes();
        var a=AddOrder(homes[0],BakingState.Ready);var b=AddOrder(homes[homes.Length>1?1:0],BakingState.Ready);
        Explain("ここは配達指示の画面です。左に焼き上がった伝票、下に配達員のレーン、地図に届け先が出ます。",()=>Parts("Orders","LaneViewport"),()=>FocusMap(homes));
        Explain("地図のピンが届け先です。ピンの色で注文の状態がわかります。黄＝受付（まだ作っていない）、青＝作っている、赤＝窯で焼いている、緑＝焼き上がり（配達できる）。ピンの番号は左の伝票の番号と同じです。",()=>Parts("Orders"));
        var score=FindFirstObjectByType<ScoreManager>();float grace=score!=null?score.LateAcceptanceSeconds:20;
        Explain("注文には「お届け目安」があります（上のバーの秒数）。注文が入ってからこの秒数以内に届けると期限内で、代金にチップが付きます。伝票に残り時間が出ます。",()=>Parts("Orders"));
        Explain("お届け目安は、上のバーのつまみで変えられます（開店準備の画面でも変えられます）。長くすると間に合いやすくなりますが、お客さんが待たされるのを嫌って注文が減ります。短くすると注文が増えますが、遅れやすくなります。",()=>Parts("PromiseSlider"));
        Explain($"遅れると、1秒ごとに代金が減り、評判（★）も下がります。期限を過ぎてから{grace:F0}秒以内に出発しないと、注文はキャンセルになります。出発した注文はキャンセルされません。",()=>Parts("Orders"));
        Add($"伝票を、配達員のレーンへドラッグしましょう（{Courier.Name} は{Courier.Capacity}枚まで運べます）。",()=>Roster.People.Any(p=>Handover.BagOf(p).Count>=1),()=>new[]{OrderCard(),Part("Lane_"+Courier.Name)});
        Add("もう1枚も同じレーンへ入れると、1回の配達でまとめて届けられます。",()=>Roster.People.Any(p=>Handover.BagOf(p).Count>=2),()=>new[]{OrderCard(),Part("Lane_"+Courier.Name)});
        Explain("地図の赤い線が、配達員が通る道です。線をつかんで別の道へドラッグすると、道順を変えられます（道をクリックしても選べます）。混んでいる道は避けましょう。");
        Add("「出発」を押しましょう。",()=>Roster.People.Any(p=>p.CurrentTrip!=null),()=>Parts("Depart_"+Courier.Name));
        Add("配達中です。「早送り」を押している間は時間が早く進みます。2軒に届くまで待ちましょう。",()=>a.Status==OrderStatus.Delivered && b.Status==OrderStatus.Delivered,()=>Parts("FastForward"));
        Explain("配達完了！ 期限内に届けると、チップと品質ボーナスがもらえます。配達員は店に戻ると、次の配達に行けます。",()=>Parts("Lane_"+Courier.Name));
    }

    /// <summary>Zooms the dispatch map onto the shop and the tutorial's homes, so the route is large enough to follow.</summary>
    private void FocusMap(string[] homes)
    {
        var map=FindFirstObjectByType<MapManager>();var cam=Camera.main;if(map==null || cam==null)return;
        var points=homes.Select(map.HomeAt).Append(map.HomeAt(map.StoreNodeId)).ToList();
        Vector2 min=points.Aggregate(Vector2.Min),max=points.Aggregate(Vector2.Max),center=(min+max)/2;
        // The map shows between the order list (left), the lanes (bottom) and the controls (right): aim a bit up and right.
        float size=Mathf.Max((max.y-min.y)*.5f/.45f,(max.x-min.x)*.5f/(.45f*cam.aspect),3f);
        cam.orthographicSize=size;
        cam.transform.position=new Vector3(center.x-size*cam.aspect*.08f,center.y-size*.22f,cam.transform.position.z);
    }

    // ---- 3. 内勤と作り直し ----
    private PizzaJob failed;private Order remake;private bool discarded;
    private System.Action<PizzaJob> onDiscard;
    private void ChapterRecovery()
    {
        OpenShop(ShopZone.Kitchen);
        onDiscard=j=>{if(remake!=null && j.Order==remake)discarded=true;};Cooking.PizzaDiscarded+=onDiscard;
        var homes=NearestHomes(2);
        var a=AddOrder(homes[0]);
        var cook=FindFirstObjectByType<KitchenCrew>().People.FirstOrDefault();string name=cook!=null?cook.Name:"内勤";
        Explain($"内勤の{name}は、伝票を自動で作ってくれます。手際が高いほど速く、丁寧さが高いほど★が付きやすくなります。",()=>Parts("Benches"));
        Add($"{name}が1枚作り終えるまで待ちましょう。",()=>a.IsReady,()=>Parts("Benches","Ovens"));
        Explain("次は、失敗したピザの扱いです。いまから、ソースが薄くて具がちがうピザを焼いてみます。");
        Add($"焼いています…（{name}は別の注文を作っています）",()=>failed!=null && failed.NeedsDecision,()=>Parts("Ovens"),()=>BakeFailedPizza(homes[0],homes[homes.Length>1?1:0]));
        steps[^1].Tick=KeepFailedPizzaWithPlayer;
        Add("失敗したピザが保温棚に出ました。ゴミ箱へドラッグすると、作り直しになります（捨てても費用はかかりません。材料費は出発のときに1枚分だけです）。",()=>discarded || remake.IsReady,()=>Parts("WarmingShelf","Trash"));
        Add($"作り直しは生地から始まります。自分で作るか、そのまま8秒待つと{name}が引き継ぎます。完成させましょう。",()=>remake.IsReady,()=>Parts("Benches"));
        Explain($"完成！ 内勤は手が空くと、放っておかれたピザを引き継ぎます。失敗したピザは「そのまま出す」（★が少ない）か、作り直すかを選べます。");
    }
    /// <summary>Makes a pizza with thin sauce and the wrong topping through the cooking API and bakes it.</summary>
    private void BakeFailedPizza(string busyHome,string home)
    {
        // The kitchen staff takes a new ticket as soon as it arrives: give them an earlier one first.
        AddOrder(busyHome,BakingState.Waiting,TutorialPromise-120);
        remake=AddOrder(home);
        int bench=Enumerable.Range(0,Cooking.BenchCount).FirstOrDefault(i=>Cooking.BenchJob(i)==null);
        if(!Cooking.Claim(remake,bench)){Debug.LogWarning("Tutorial: could not start the failed pizza.");return;}
        failed=Cooking.JobFor(remake);
        Cooking.AddDough(bench);Cooking.AddSauce(bench,.1f);Cooking.AddCheese(bench);
        Cooking.AddTopping(bench,remake.RequiredTopping==Topping.Pepperoni?Topping.Basil:Topping.Pepperoni);
        Cooking.PutInOven(bench,0);
    }
    /// <summary>Keeps the kitchen staff from taking the failed pizza over, and takes it out when it is baked.</summary>
    private void KeepFailedPizzaWithPlayer()
    {
        if(failed==null || failed.Worker!=PizzaWorker.Player)return;
        Cooking.Touch(failed);
        if(failed.Stage==PizzaStage.Baking && failed.Oven>=0 && Cooking.OvenElapsed(failed.Oven)>=Cooking.BandStartFor(remake)+.5f)Cooking.TakeOut(failed.Oven);
    }
}
