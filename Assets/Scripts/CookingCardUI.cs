using UnityEngine;
using UnityEngine.UI;
using TMPro;
public sealed class CookingCardUI : MonoBehaviour
{
    [SerializeField] private RectTransform fill,perfectBand;
    [SerializeField] private Button takeOut;
    [SerializeField] private TextMeshProUGUI quality;
    private Order bound;
    public Order BoundOrder=>bound;
    private CookingManager cooking;
    public void Show(Order order)
    {
        if(cooking==null)cooking=FindFirstObjectByType<CookingManager>();
        if(bound!=order){bound=order;takeOut.onClick.RemoveAllListeners();takeOut.onClick.AddListener(()=>cooking.TryTakeOut(bound));}
        float progress=cooking.Progress(order);fill.anchorMax=new Vector2(progress,1);perfectBand.anchorMin=new Vector2(cooking.PerfectStart,0);perfectBand.anchorMax=new Vector2(cooking.PerfectEnd,1);
        UIVisibility.Set(takeOut.gameObject,order.Baking==BakingState.Cooking);takeOut.interactable=cooking.ManualControlEnabled && progress>=cooking.PerfectStart && progress<1;
        quality.text=order.IsReady?(order.Quality==BakeQuality.Perfect?"Perfect":"Standard"):$"{progress:P0}";
    }
}
