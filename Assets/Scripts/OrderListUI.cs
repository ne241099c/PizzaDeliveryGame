using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderListUI : MonoBehaviour
{
    [SerializeField] private Transform orderListContent;
    [SerializeField] private Button orderItemPrefab;
    [SerializeField] private GameClock clock;
    [SerializeField] private TextMeshProUGUI emptyMessage;
    [SerializeField, Min(62)] private float rowHeight=110;
    [SerializeField, Min(0f)] private float urgentSeconds = 15f;
    private readonly List<Order> orders = new();
    private readonly List<Button> orderButtons = new();
    private readonly List<Order> selectedOrders = new();
    public Order SelectedOrder => selectedOrders.FirstOrDefault();
    public IReadOnlyList<Order> SelectedOrders => selectedOrders;
    public IReadOnlyList<Order> Orders => orders;
    public int OrderCount => orders.Count;
    public event Action SelectionChanged;
    public event Action OrdersChanged;

    private void Start()
    {
        orderButtons.AddRange(orderListContent.GetComponentsInChildren<Button>(true));
        Refresh();
    }
    private void Update()
    {
        for (int i = 0; i < orders.Count; i++) UpdateLabel(orderButtons[i], orders[i]);
        if (emptyMessage != null && orders.Count == 0)
            emptyMessage.text = clock.Phase == DayPhase.Open ? "Waiting for new orders..." : "No waiting orders.";
    }
    public void AddOrder(Order order)
    {
        if (order == null || order.Status != OrderStatus.Pending || orders.Contains(order)) return;
        orders.Add(order);
        Refresh();
        OrdersChanged?.Invoke();
    }
    public bool Contains(Order order) => orders.Contains(order) && order.Status == OrderStatus.Pending;
    public void RemoveOrder(Order order) => RemoveOrders(new[] { order });
    public void RemoveOrders(IEnumerable<Order> removed)
    {
        bool selectionChanged = false;
        foreach (Order order in removed.ToArray())
        {
            orders.Remove(order);
            selectionChanged |= selectedOrders.Remove(order);
        }
        Refresh();
        OrdersChanged?.Invoke();
        if (selectionChanged) SelectionChanged?.Invoke();
    }
    public void ClearSelection()
    {
        if (selectedOrders.Count == 0) return;
        selectedOrders.Clear();
        Refresh();
        SelectionChanged?.Invoke();
    }
    private void Refresh()
    {
        while (orderButtons.Count < orders.Count)
        {
            Button button = Instantiate(orderItemPrefab, orderListContent);
            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = rowHeight;
            button.GetComponentInChildren<TextMeshProUGUI>().raycastTarget = false;
            orderButtons.Add(button);
        }
        for (int i = 0; i < orderButtons.Count; i++)
        {
            Button button = orderButtons[i];
            button.gameObject.SetActive(i < orders.Count);
            if (i >= orders.Count) continue;
            Order order = orders[i];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleOrder(order));
            UpdateLabel(button, order);
        }
        if (emptyMessage != null) emptyMessage.gameObject.SetActive(orders.Count == 0);
    }
    public bool CanSelect(Order order)=>Contains(order) && order.IsReady && clock.ElapsedSeconds<order.Deadline;
    private void UpdateLabel(Button button, Order order)
    {
        var layout=button.GetComponent<LayoutElement>();if(layout!=null)layout.minHeight=layout.preferredHeight=rowHeight;
        float remaining = order.Deadline - clock.ElapsedSeconds;
        string deadline = remaining >= 0 ? $"{remaining:F0}s left" : $"LATE {-remaining:F0}s";
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        string baking=order.Baking==BakingState.Waiting?"Waiting for oven":order.Baking==BakingState.Cooking?$"Cooking {Mathf.Max(0,order.CookFinishesAt-clock.ElapsedSeconds):F1}s":"Ready";
        label.text = $"{order.DestinationName}\n<size=17>{deadline} / {baking}</size>";
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,8);label.rectTransform.offsetMax=new Vector2(-8,-8);
        label.fontSize = 20;
        label.color = new Color(0.1f, 0.14f, 0.2f);
        button.GetComponent<Image>().color = selectedOrders.Contains(order) ? Color.yellow
            : remaining <= urgentSeconds ? new Color(1f, 0.62f, 0.48f) : Color.white;
        button.interactable = clock.Phase == DayPhase.Open && CanSelect(order);
    }
    public bool SelectOnly(Order order) => SelectForDispatch(new[]{order});
    public bool SelectForDispatch(IEnumerable<Order> selection)
    {
        var chosen=selection?.ToArray();
        if(clock.Phase!=DayPhase.Open || chosen==null || chosen.Length<1 || chosen.Length>2 || chosen.Distinct().Count()!=chosen.Length || chosen.Any(o=>!CanSelect(o)))return false;
        selectedOrders.Clear();selectedOrders.AddRange(chosen);Refresh();SelectionChanged?.Invoke();return true;
    }
    private void ToggleOrder(Order order)
    {
        if (clock.Phase != DayPhase.Open || !CanSelect(order)) return;
        if (!selectedOrders.Remove(order))
        {
            if (selectedOrders.Count >= 2) return;
            selectedOrders.Add(order);
        }
        Refresh();
        SelectionChanged?.Invoke();
    }
}
