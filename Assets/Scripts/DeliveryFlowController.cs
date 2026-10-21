using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryFlowController : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private OrderListUI orderListUI;
    [SerializeField] private DeliveryPersonSelector deliveryPersonSelector;
    [SerializeField] private Button completeDeliveryButton;
    private DeliveryPlannerUI planner;

    public void ConfigureUI(Button completeButton, DeliveryPlannerUI planUI)
    {
        completeDeliveryButton = completeButton;
        planner = planUI;
    }

    private void Start()
    {
        completeDeliveryButton.onClick.AddListener(CompleteDelivery);
        UpdateCompleteButtonState();
    }

    private void Update() => UpdateCompleteButtonState();

    private bool CanComplete()
    {
        DeliveryPlan plan = planner != null ? planner.SelectedPlan : null;
        return plan != null && deliveryPersonSelector.SelectedPerson != null
            && deliveryPersonSelector.SelectedPerson.IsAvailable
            && plan.Orders.Count == orderListUI.SelectedOrders.Count
            && plan.Orders.All(o => orderListUI.SelectedOrders.Contains(o));
    }

    private void CompleteDelivery()
    {
        if (!CanComplete()) return;
        DeliveryPlan plan = planner.SelectedPlan;
        float? individualReferenceTime = planner.IndividualReferenceTime;
        foreach (Order order in plan.Orders) scoreManager.OnDeliveryCompleted();
        orderListUI.RemoveOrders(plan.Orders);
        deliveryPersonSelector.ClearSelection();
        planner.ShowCompletion(plan, individualReferenceTime);
        UpdateCompleteButtonState();
    }

    private void UpdateCompleteButtonState()
    {
        if (completeDeliveryButton != null) completeDeliveryButton.interactable = CanComplete();
    }
}
