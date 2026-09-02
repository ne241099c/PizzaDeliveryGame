using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryPersonSelector : MonoBehaviour
{
    [SerializeField] private Transform deliveryPersonListContent;
    [SerializeField] private Button deliveryPersonItemPrefab;

    private readonly List<DeliveryPerson> deliveryPeople = new();
    private readonly List<Button> deliveryPersonButtons = new();

    public DeliveryPerson SelectedPerson { get; private set; }

    private void Start()
    {
        CreateDummyDeliveryPeople();
        Refresh();
    }

    private void CreateDummyDeliveryPeople()
    {
        deliveryPeople.Add(new DeliveryPerson("Minato", 0.9f));
        deliveryPeople.Add(new DeliveryPerson("Aoi", 0.6f));
        deliveryPeople.Add(new DeliveryPerson("Ren", 0.3f));
    }

    private void Refresh()
    {
        EnsureButtonCount(deliveryPeople.Count);

        for (int i = 0; i < deliveryPersonButtons.Count; i++)
        {
            Button button = deliveryPersonButtons[i];
            bool hasPerson = i < deliveryPeople.Count;
            button.gameObject.SetActive(hasPerson);

            if (!hasPerson)
            {
                continue;
            }

            DeliveryPerson person = deliveryPeople[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = person.Name;

            button.onClick.RemoveAllListeners();
            DeliveryPerson capturedPerson = person;
            button.onClick.AddListener(() => TogglePerson(capturedPerson));
            UpdateButtonColor(button, person);
        }
    }

    private void EnsureButtonCount(int requiredCount)
    {
        while (deliveryPersonButtons.Count < requiredCount)
        {
            Button button = Instantiate(deliveryPersonItemPrefab, deliveryPersonListContent);
            deliveryPersonButtons.Add(button);
        }
    }

    private void TogglePerson(DeliveryPerson person)
    {
        if (SelectedPerson == person)
        {
            SelectedPerson = null;
        }
        else
        {
            SelectedPerson = person;
        }

        Refresh();
    }

    private void UpdateButtonColor(Button button, DeliveryPerson person)
    {
        Image image = button.GetComponent<Image>();

        if (SelectedPerson == person)
        {
            image.color = Color.cyan;
        }
        else
        {
            image.color = Color.white;
        }
    }

    public void ClearSelection()
    {
        SelectedPerson = null;
        Refresh();
    }
}
