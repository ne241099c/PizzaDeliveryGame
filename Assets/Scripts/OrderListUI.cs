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

    public Order SelectedOrder { get; private set; }

    private void Start()
    {
        CreateDummyOrders();
        Refresh();
    }

    public void RemoveOrder(Order order)
    {
        orders.Remove(order);

        if (SelectedOrder == order)
        {
            SelectedOrder = null;
        }

        Refresh();
    }

    private void CreateDummyOrders()
    {
        orders.Add(new Order("Tanaka House", Time.time, 60f));
        orders.Add(new Order("Suzuki Apartment", Time.time, 60f));
        orders.Add(new Order("Sato Office", Time.time, 60f));
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
            label.text = order.DestinationName;

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
            orderButtons.Add(button);
        }
    }

    private void ToggleOrder(Order order)
    {
        if (SelectedOrder == order)
        {
            SelectedOrder = null;
        }
        else
        {
            SelectedOrder = order;
        }

        Refresh();
    }

    private void UpdateButtonColor(Button button, Order order)
    {
        Image image = button.GetComponent<Image>();
        if (SelectedOrder == order)
        {
            image.color = Color.yellow;
        }
        else
        {
            image.color = Color.white;
        }
    }
}
