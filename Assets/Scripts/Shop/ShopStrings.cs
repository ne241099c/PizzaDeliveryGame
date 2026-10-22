using System.Collections.Generic;

/// <summary>Japanese text for the redesigned screens, kept in one place.
/// (UiStrings.cs translates the older screens and is maintained separately.)</summary>
public static class ShopStrings
{
    public static readonly string[] AbilityNames={"運転","土地勘","接客","体力","安全性","積載量"};
    public static string Ability(CourierAbility a)=>AbilityNames[(int)a];
    public static string Archetype(RecruitArchetype a)=>a switch{RecruitArchetype.Newcomer=>"新人",RecruitArchetype.Experienced=>"即戦力",_=>"標準"};
    public static string Topping(Topping t)=>t switch{global::Topping.Pepperoni=>"ペパロニ",global::Topping.Shrimp=>"えび",_=>"バジル"};
    public static string Status(DeliveryStatus s)=>s switch{DeliveryStatus.Delivering=>"配達中",DeliveryStatus.Returning=>"帰還中",DeliveryStatus.Training=>"訓練中",_=>"待機"};
    public static string Facility(FacilityType f)=>f switch{FacilityType.Oven=>"窯",FacilityType.Bike=>"バイク",FacilityType.Expansion=>"店舗の拡張",FacilityType.Workbench=>"作業台",_=>"広告"};

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

    public const string Day="日目",Preparation="開店準備",Open="営業中",Closed="閉店",Ended="閉店 / 全員帰還";
    public const string Staff="スタッフ",Applicants="求人",Facilities="設備",Management="経営";
    public const string Courier="配達員",Kitchen="内勤",Train="訓練",CancelTraining="訓練を取り消す",Fire="解雇",Hire="雇う",Wage="日給";
    public const string StartBusiness="営業開始",Promise="お届け目安",Seconds="秒";
    public const string TeamNote="上限 {0}人は、配達員と内勤の合計です。店舗の拡張で増やせます。";
    public const string KitchenInfo="厨房を任せられます。手際が高いほど速く、丁寧さが高いほど★が付きやすくなります。";
    public const string Speed="手際",Care="丁寧さ";
    public const string FindTeammate="仲間を探す",SeeApplicants="求人を見る";
    public const string KitchenTab="厨房",HandoverTab="受け渡し",DispatchTab="配達指示";
    public const string Step1="配達員をえらぶ",Step2="注文の「積む」を押す",Step3="地図で届け先を順番にタップ",Step4="出発!";
    public const string Depart="出発",Suggested="おすすめ経路",Redo="やり直す",Store="お店";
}
