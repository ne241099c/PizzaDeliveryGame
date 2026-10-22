using UnityEngine;
using UnityEngine.UI;
[DefaultExecutionOrder(1100)]
public sealed class OrderDeadlineView : MonoBehaviour
{
    private RectTransform fill;private Image image;private CookingCardUI card;private GameClock clock;
    private void Awake()
    {
        card=GetComponent<CookingCardUI>();clock=FindFirstObjectByType<GameClock>();
        var go=new GameObject("DeadlineBand",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);fill=(RectTransform)go.transform;fill.anchorMin=new Vector2(0,1);fill.anchorMax=Vector2.one;fill.pivot=new Vector2(0,1);fill.offsetMin=fill.offsetMax=Vector2.zero;fill.sizeDelta=new Vector2(0,UITheme.Active.smallSpace/2f);image=go.GetComponent<Image>();image.raycastTarget=false;
    }
    private void LateUpdate()
    {
        var order=card.BoundOrder;if(order==null){image.enabled=false;return;}image.enabled=true;
        float ratio=Mathf.Clamp01((order.Deadline-clock.ElapsedSeconds)/Mathf.Max(.01f,order.TimeLimit));fill.anchorMax=new Vector2(ratio,1);image.color=UITheme.Active.good;
    }
}
