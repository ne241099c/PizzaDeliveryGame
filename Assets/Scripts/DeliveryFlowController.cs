using UnityEngine;
using UnityEngine.UI;

public class DeliveryFlowController : MonoBehaviour
{
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] private OrderListUI orderListUI;
    [SerializeField] private DeliveryPersonSelector deliveryPersonSelector;
    [SerializeField] private Button completeDeliveryButton;

    private void Start()
    {
        completeDeliveryButton.onClick.AddListener(CompleteDelivery);
        UpdateCompleteyButtonState();
    }

    private void Update()
    {
        UpdateCompleteyButtonState();
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

        UpdateCompleteyButtonState();
    }

    private void UpdateCompleteyButtonState()
    {
        bool hasSelectedOrder = orderListUI.SelectedOrder != null;
        bool hasSelectedPerson = deliveryPersonSelector.SelectedPerson != null;

        completeDeliveryButton.interactable = hasSelectedOrder && hasSelectedPerson;
    }
}