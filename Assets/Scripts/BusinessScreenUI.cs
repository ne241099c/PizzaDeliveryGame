using UnityEngine;
using UnityEngine.UI;
public sealed class BusinessScreenUI : MonoBehaviour
{
    [SerializeField] private ScreenRouter router;
    [SerializeField] private GameClock clock;
    [SerializeField] private OrderListUI orders;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField] private CanvasGroup drawer,ordersBand,couriersBand;
    [SerializeField] private RectTransform mapFrame;
    [SerializeField] private Button deselect,minus,plus;
    [SerializeField] private OrderSpawner spawner;
    [SerializeField,Min(0)] private float slideSeconds=.2f;
    [SerializeField,Range(.5f,1)] private float planningZoom=.94f;
    [SerializeField,Range(.1f,1)] private float planningBandAlpha=.75f;
    [SerializeField,Min(0)] private float promiseStep=10;
    private bool open;
    public bool IsPlanningPanelOpen=>open;
    private void Start()
    {
        deselect.onClick.AddListener(()=>{orders.ClearSelection();roster.ClearSelection();});
        minus.onClick.AddListener(()=>spawner.SetPromiseSeconds(spawner.PromiseSeconds-promiseStep));
        plus.onClick.AddListener(()=>spawner.SetPromiseSeconds(spawner.PromiseSeconds+promiseStep));
    }
    private void LateUpdate()=>Refresh();
    public void Refresh()
    {
        bool next=router.Current==ScreenId.Business && clock.Phase==DayPhase.Open && orders.SelectedOrders.Count>0 && roster.SelectedPerson!=null && roster.SelectedPerson.IsAvailable;
        if(next!=open)
        {
            open=next;UIVisibility.Set(drawer.gameObject,open);
            if(Camera.main!=null)Camera.main.orthographicSize*=open?planningZoom:1/planningZoom;
        }
        var r=(RectTransform)drawer.transform;
        float target=open?1504:1528;
        r.anchoredPosition=new Vector2(Mathf.Lerp(r.anchoredPosition.x,target,slideSeconds<=0?1:Mathf.Clamp01(Time.unscaledDeltaTime/slideSeconds)),r.anchoredPosition.y);
        mapFrame.sizeDelta=new Vector2(open?1152:1568,610);
        ordersBand.alpha=couriersBand.alpha=open?planningBandAlpha:1;
    }
}
