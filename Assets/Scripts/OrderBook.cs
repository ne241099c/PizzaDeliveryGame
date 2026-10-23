using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Today's waiting orders and the ones selected for the next dispatch. Data only: the dispatch board
/// (BusinessView) shows them.</summary>
public class OrderBook : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    private readonly List<Order> orders=new();
    private readonly List<Order> selectedOrders=new();
    private ScoreManager score;
    public Order SelectedOrder=>selectedOrders.FirstOrDefault();
    public IReadOnlyList<Order> SelectedOrders=>selectedOrders;
    public IReadOnlyList<Order> Orders=>orders;
    public int OrderCount=>orders.Count;
    public event Action SelectionChanged;
    public event Action OrdersChanged;

    public void AddOrder(Order order)
    {
        if(order==null || order.Status!=OrderStatus.Pending || orders.Contains(order))return;
        orders.Add(order);
        OrdersChanged?.Invoke();
    }
    public bool Contains(Order order)=>orders.Contains(order) && order.Status==OrderStatus.Pending;
    public void RemoveOrder(Order order)=>RemoveOrders(new[]{order});
    public void RemoveOrders(IEnumerable<Order> removed)
    {
        bool selectionChanged=false;
        foreach(Order order in removed.ToArray())
        {
            orders.Remove(order);
            selectionChanged|=selectedOrders.Remove(order);
        }
        OrdersChanged?.Invoke();
        if(selectionChanged)SelectionChanged?.Invoke();
    }
    public void ClearSelection()
    {
        if(selectedOrders.Count==0)return;
        selectedOrders.Clear();
        SelectionChanged?.Invoke();
    }
    /// <summary>Ready (boxed) and still inside the late-acceptance window.</summary>
    public bool CanSelect(Order order)
    {
        if(score==null)score=FindFirstObjectByType<ScoreManager>();
        return Contains(order) && order.IsReady && clock.ElapsedSeconds<score.CancellationAt(order);
    }
    public bool SelectOnly(Order order)=>SelectForDispatch(new[]{order});
    /// <summary>One or two different ready orders, while the shop is open.</summary>
    public bool SelectForDispatch(IEnumerable<Order> selection)
    {
        var chosen=selection?.ToArray();
        if(clock.Phase!=DayPhase.Open || chosen==null || chosen.Length<1 || chosen.Length>2)return false;
        if(chosen.Distinct().Count()!=chosen.Length || chosen.Any(o=>!CanSelect(o)))return false;
        selectedOrders.Clear();
        selectedOrders.AddRange(chosen);
        SelectionChanged?.Invoke();
        return true;
    }
}
