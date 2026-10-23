using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The management tab: the delivery promise (slider, value, effect) and the days so far as rows with a profit bar.
/// Layout: Assets/Prefabs/UI/Cards/ManagementPanel.prefab; the rows follow the records and are built here.</summary>
public sealed class ManagementPanelView : MonoBehaviour
{
    [SerializeField] private Slider promise;
    [SerializeField] private TextMeshProUGUI promiseValue,promiseDetail,totals,empty;
    [SerializeField] private RectTransform rows;
    public Slider Promise=>promise;
    public void ShowPromise(string value,string detail){promiseValue.text=value;promiseDetail.text=detail;}
    public void ShowHistory(string totalsText,System.Collections.Generic.IReadOnlyList<(int day,decimal revenue,decimal profit,decimal cash)> shown)
    {
        var T=UiKit.T;totals.text=totalsText;empty.gameObject.SetActive(shown.Count==0);
        for(int i=rows.childCount-1;i>=0;i--){var old=rows.GetChild(i);old.SetParent(null,false);Destroy(old.gameObject);}
        decimal scale=1;foreach(var r in shown)scale=System.Math.Max(scale,System.Math.Abs(r.profit));
        for(int i=0;i<shown.Count;i++)
        {
            var r=shown[i];float y=i*62;
            UiKit.Label(rows,"Day"+i,r.day+ShopStrings.Day,0,y,100,48,22,T.line,FontRole.Black);
            UiKit.Label(rows,"Revenue"+i,"売上 "+UiKit.Yen(r.revenue),102,y,220,48,20,T.line,FontRole.Bold);
            float width=(float)(System.Math.Abs(r.profit)/scale)*260;var bar=UiKit.Box(rows,"Bar"+i,332,y+14,Mathf.Max(6,width),22);UiKit.Fill(bar,r.profit>=0?T.basil:T.berry,8);
            UiKit.Label(rows,"Profit"+i,UiKit.Signed(r.profit),612,y,130,48,22,r.profit>=0?T.basil:T.berry,FontRole.Black,TextAlignmentOptions.MidlineRight);
            UiKit.Label(rows,"Cash"+i,UiKit.Yen(r.cash),752,y,130,48,20,T.inkSub,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        }
    }
}
