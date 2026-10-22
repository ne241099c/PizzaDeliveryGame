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
        bound=order;takeOut.onClick.RemoveAllListeners();
        float progress=cooking.Progress(order);fill.anchorMax=new Vector2(progress,1);perfectBand.anchorMin=perfectBand.anchorMax=Vector2.zero;
        UIVisibility.Set(takeOut.gameObject,false);
        quality.text=order.IsReady?new string('★',Mathf.Max(1,order.Stars)):cooking.StageCaption(order);
    }
}
