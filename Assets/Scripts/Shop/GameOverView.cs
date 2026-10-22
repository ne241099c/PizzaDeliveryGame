using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>閉店 (設計書_画面デザイン 4-4): a calm summary and two choices.</summary>
public sealed class GameOverView : MonoBehaviour
{
    private ScreenRouter router;private CampaignManager campaign;private DeliveryPersonSelector roster;private KitchenCrew crew;private CampaignSaveManager save;
    private UITheme T=>UiKit.T;private RectTransform card;private ScreenId last;
    private void Awake()
    {
        router=FindFirstObjectByType<ScreenRouter>();campaign=FindFirstObjectByType<CampaignManager>();roster=FindFirstObjectByType<DeliveryPersonSelector>();crew=FindFirstObjectByType<KitchenCrew>();save=FindFirstObjectByType<CampaignSaveManager>();
        var root=(RectTransform)transform;UiKit.Fill(UiKit.Stretch(root,"Background"),new Color(.36f,.27f,.22f),0,true);
        for(int y=0;y<1080;y+=96){var s=UiKit.Box(root,"Stripe",0,y,1920,3);UiKit.Fill(s,new Color(1,1,1,.05f),0);}
    }
    private void Update()
    {
        if(UIThemeDriver.RenderingSuspended)return;
        if(router.Current==last)return;last=router.Current;if(last!=ScreenId.GameOver)return;
        if(card!=null)Destroy(card.gameObject);
        card=UiKit.Card(transform,"Card",460,150,1000,760,T.paper,24,8,3);
        UiKit.Label(card,"Title","閉店しました",0,46,1000,90,64,T.line,FontRole.Display,TextAlignmentOptions.Center);
        UiKit.Label(card,"Days",$"{campaign.DayNumber}日目まで、お店を続けました。",0,140,1000,40,26,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
        string[] heads={"通算売上","最高の日の売上","配達した数"};string[] values={UiKit.Yen(campaign.TotalRevenue),UiKit.Yen(campaign.BestDailyRevenue),campaign.TotalDelivered+"件"};
        for(int i=0;i<3;i++){var c=UiKit.Card(card,"Stat"+i,60+i*300,210,280,150,T.paperDeep,16,0,2);UiKit.Label(c,"Head",heads[i],0,18,280,32,20,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);UiKit.Label(c,"Value",values[i],0,58,280,70,44,T.line,FontRole.Display,TextAlignmentOptions.Center);}
        UiKit.Label(card,"TeamTitle","育てたスタッフ",60,392,600,40,26,T.line,FontRole.Display);
        float x=60;foreach(var p in roster.People.Take(6)){UiKit.Avatar(card,"Avatar_"+p.Name,x,444,76,T.CourierColor(p.Name),p.Name.Substring(0,1));UiKit.Label(card,"Name_"+p.Name,p.Name,x-20,526,116,30,18,T.line,FontRole.Black,TextAlignmentOptions.Center);x+=128;}
        foreach(var c in crew.People.Take(2)){UiKit.Avatar(card,"Avatar_"+c.Name,x,444,76,T.info,c.Name.Substring(0,1));UiKit.Label(card,"Name_"+c.Name,c.Name,x-20,526,116,30,18,T.line,FontRole.Black,TextAlignmentOptions.Center);x+=128;}
        if(save!=null)UiKit.Label(card,"Records",save.RecordsSummary,60,566,880,36,18,T.inkSub,FontRole.Black);
        UiKit.Button(card,"Retry","もう一度",StickerKind.Primary,520,620,420,100,40,()=>save.StartNewGame(),FontRole.Display);
        UiKit.Button(card,"ToTitle","タイトルへ",StickerKind.Secondary,60,630,300,84,28,()=>router.Show(ScreenId.Title),FontRole.Display);
    }
}
