using System.Collections.Generic;

/// <summary>Japanese text for the redesigned screens, kept in one place.
/// (UiStrings.cs translates the older screens and is maintained separately.)</summary>
public static class ShopStrings
{
    public const string GrowthTitle="成長・経験",GrowthMaximum="素質の上限",GrowthEmpty="配達で経験がたまります。裏道では土地勘も育ちます。";
    public static string GrowthExperience(CourierAbility ability)=>"経験 "+Ability(ability);
    public static string GrowthNext(float fraction,float gain)=>$"{fraction:P0} → +{gain:0.00}";
    public static string GrowthProgress(string name,CourierAbility ability)=>name+" ・ "+Ability(ability);
    public static string GrowthProgressValue(float fraction)=>$"次の成長まで {fraction:P0}";
    public static string GrowthCount(int increases,int progressing)=>$"成長 {increases} / 経験 {progressing}人";
    public static string FacilityLevel(int current,int maximum)=>$"Lv {current} / {maximum}";
    public const string FacilityCompleted="設備を最大まで強化済み";
    public const string MenuCompleted="すべてのピザを解禁済み";
    public static readonly string[] AbilityNames={"運転","土地勘","接客","体力","安全性","積載量"};
    public static string Ability(CourierAbility a)=>AbilityNames[(int)a];
    public static string Archetype(RecruitArchetype a)=>a switch{RecruitArchetype.Newcomer=>"新人",RecruitArchetype.Experienced=>"即戦力",_=>"標準"};
    public static string Topping(Topping t)=>t switch{global::Topping.Pepperoni=>"ペパロニ",global::Topping.Shrimp=>"えび",global::Topping.Squid=>"いか",_=>"バジル"};
    public static string Status(DeliveryStatus s)=>s switch{DeliveryStatus.Delivering=>"配達中",DeliveryStatus.Returning=>"帰還中",DeliveryStatus.Training=>"訓練中",_=>"待機"};
    public static string Facility(FacilityType f)=>f switch{FacilityType.Oven=>"窯",FacilityType.Bike=>"バイク",FacilityType.Expansion=>"店舗の拡張",FacilityType.Workbench=>"作業台",FacilityType.Menu=>"メニュー拡張",_=>"広告"};

    private static readonly string[] Houses={"田中さん宅","佐藤さん宅","鈴木荘","高橋さん宅","伊藤さん宅","山田さん宅","中村さん宅","小林ハイツ"};
    private static readonly Dictionary<string,string> houseByNode=new();
    /// <summary>Fixed display name per delivery node (a map label, independent of individual orders).</summary>
    public static string House(MapManager map,string nodeId)
    {
        if(houseByNode.TryGetValue(nodeId,out var name))return name;
        int index=0,i=0;
        foreach(var node in map.Nodes){if(node.NodeType!=NodeType.DeliveryAddress || node.IsStore)continue;if(node.Id==nodeId){index=i;break;}i++;}
        name=Houses[index%Houses.Length]+(index>=Houses.Length?(index/Houses.Length+1).ToString():"");houseByNode[nodeId]=name;return name;
    }

    public const string FailureWaiting="失敗・選択待ち";
    public const string Trash="ゴミ箱",TrashHint="ピザ・箱をここにドロップ",Redo="作り直し",ServeAsIs="このまま出す",DiscardRedo="捨てて作り直す";
    public const string Undercooked="生焼け：窯へ戻せます",Burnt="焦げた！★1・ボーナスなし",WrongIngredients="伝票と具が違います",ThinSauce="ソース不足",Overbaked="焼きのタイミング失敗";
    public const string Day="日目",Preparation="開店準備",Open="営業中",Closed="閉店",Ended="閉店 / 全員帰還";
    public const string Staff="スタッフ",Applicants="求人",Facilities="設備",Management="経営";
    public const string Courier="配達員",Kitchen="内勤",Train="訓練",CancelTraining="訓練を取り消す",Fire="解雇",Hire="雇う",Wage="日給";
    public const string StartBusiness="営業開始",Promise="お届け目安",Seconds="秒";
    public const string TeamNote="従業員の人数制限はありません。日給と設備の数を見て採用しましょう。";
    public const string KitchenInfo="厨房を任せられます。手際が高いほど速く、丁寧さが高いほど★が付きやすくなります。";
    public const string Speed="手際",Care="丁寧さ";
    public const string FindTeammate="仲間を探す",SeeApplicants="求人を見る";
    public const string KitchenTab="厨房",HandoverTab="受け渡し",DispatchTab="配達指示";
    public const string Step1="伝票を配達員のレーンへ",Step2="ルートを直して出発";
    public const string Depart="出発",ResetRoute="自動に戻す",Store="お店";
    public static string InstantTrainingResult(string name,string ability)=>$"{name} の{ability}がすぐに上がりました。営業を休みません。";
    public const string InstantTrainingNoRefund="購入した訓練はすでに反映されています。取り消し・返金はできません。";
    public const string CourierTrainingNote="購入後すぐに能力アップ。営業を休みません。上限と資金の範囲で何度でも購入できます。返金はできません。",KitchenTrainingNote="購入後すぐに能力アップ。営業を休みません。上限と資金の範囲で何度でも購入できます。返金はできません。";
    public static string KitchenSkill(KitchenSkill skill)=>skill==global::KitchenSkill.Speed?Speed:Care;
    public const string FastForward="押している間 早送り",RoadTipVia="クリック：この道を通る",RoadTipUndo="クリック：この道をやめる";
    public const string SwapDeliveryOrder="順番を入替",ChooseRoad="道を選ぶ",OvenQueueTitle="窯待ち →";
    public const string FreeDiscard="ゴミ箱（廃棄の減点なし）";
    public const string RoadEditHint="：通りたい道をクリック。同じボタンで区間切替、右クリックで終了。";
    public const string RouteControlsHint="荷物をクリックで降ろす／「順番を入替」で届ける順を変更。「道を選ぶ」を押して、通りたい道をクリックします。";
    public static string RoadSection(int number)=>$"道変更：区間{number}";
}
