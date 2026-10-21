using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderListUI : MonoBehaviour
{
    [SerializeField] private Transform orderListContent;
    [SerializeField] private Button orderItemPrefab;

    private readonly List<Order> orders = new();
    private readonly List<Button> orderButtons = new();

    private readonly List<Order> selectedOrders = new();
    public Order SelectedOrder => selectedOrders.Count == 0 ? null : selectedOrders[0];
    public IReadOnlyList<Order> SelectedOrders => selectedOrders.AsReadOnly();
    public int OrderCount => orders.Count;
    public event Action SelectionChanged;

    public void ConfigureContent(Transform content) => orderListContent = content;

    private void Start()
    {
        CreateDummyOrders();
        Refresh();
        SelectionChanged?.Invoke();
    }

    public void RemoveOrder(Order order)
    {
        orders.Remove(order);

        selectedOrders.Remove(order);

        Refresh();
        SelectionChanged?.Invoke();
    }

    public void RemoveOrders(IEnumerable<Order> completedOrders)
    {
        foreach (Order order in new List<Order>(completedOrders))
        {
            orders.Remove(order);
            selectedOrders.Remove(order);
        }
        Refresh();
        SelectionChanged?.Invoke();
    }

    private void CreateDummyOrders()
    {
        orders.Add(new Order("Goal1", "Tanaka House", Time.time, 60f));
        orders.Add(new Order("Goal2", "Suzuki Apartment", Time.time, 60f));
    }

    private void Refresh()
    {
        EnsureButtonCount(orders.Count);

        for (int i = 0; i < orderButtons.Count; i++)
        {
            Button button = orderButtons[i];
            bool hasOrder = i < orders.Count;
            button.gameObject.SetActive(hasOrder);

            if (!hasOrder)
            {
                continue;
            }

            Order order = orders[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{order.DestinationName}\n<size=17>{order.DestinationNodeId}</size>";
            label.fontSize = 22;
            label.color = new Color(0.12f, 0.16f, 0.23f);
            label.raycastTarget = false;

            button.onClick.RemoveAllListeners();
            Order capturedOrder = order;
            button.onClick.AddListener(() => ToggleOrder(capturedOrder));
            UpdateButtonColor(button, order);
        }
    }

    private void EnsureButtonCount(int requiredCount)
    {
        while (orderButtons.Count < requiredCount)
        {
            Button button = Instantiate(orderItemPrefab, orderListContent);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 66f;
            layout.minHeight = 66f;
            orderButtons.Add(button);
        }
    }

    private void ToggleOrder(Order order)
    {
        if (selectedOrders.Contains(order))
        {
            selectedOrders.Remove(order);
        }
        else
        {
            if (selectedOrders.Count == 2) return;
            selectedOrders.Add(order);
        }

        Refresh();
        SelectionChanged?.Invoke();
    }

    private void UpdateButtonColor(Button button, Order order)
    {
        Image image = button.GetComponent<Image>();
        if (selectedOrders.Contains(order))
        {
            image.color = Color.yellow;
        }
        else
        {
            image.color = Color.white;
        }
    }
}
