using UnityEngine;
using UnityEngine.UI;

public class DeliveryFlowController : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private OrderListUI orderListUI;
    [SerializeField] private DeliveryPersonSelector deliveryPersonSelector;
    [SerializeField] private Button completeDeliveryButton;

    private void Start()
    {
        completeDeliveryButton.onClick.AddListener(CompleteDelivery);
        UpdateCompleteButtonState();
    }

    private void Update()
    {
        UpdateCompleteButtonState();
    }

    private void CompleteDelivery()
    {
        Order selectedOrder = orderListUI.SelectedOrder;
        DeliveryPerson selectedPerson = deliveryPersonSelector.SelectedPerson;

        if (selectedOrder == null || selectedPerson == null)
        {
            Debug.LogWarning("Please select both an order and a delivery person!");
            return;
        }

        scoreManager.OnDeliveryCompleted();
        orderListUI.RemoveOrder(selectedOrder);
        deliveryPersonSelector.ClearSelection();

        UpdateCompleteButtonState();
    }

    private void UpdateCompleteButtonState()
    {
        bool hasSelectedOrder = orderListUI.SelectedOrder != null;
        bool hasSelectedPerson = deliveryPersonSelector.SelectedPerson != null;

        completeDeliveryButton.interactable = hasSelectedOrder && hasSelectedPerson;
    }
}
