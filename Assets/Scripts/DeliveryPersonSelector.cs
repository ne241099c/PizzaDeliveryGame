using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryPersonSelector : MonoBehaviour
{
    [SerializeField] private Transform deliveryPersonListContent;
    [SerializeField] private Button deliveryPersonItemPrefab;
    [SerializeField] private GameClock clock;
    [SerializeField] private List<DeliveryPerson> deliveryPeople = new()
    {
        new DeliveryPerson("Minato", 0.8f, 0.9f, 1, 0.4f, 0.6f, 0.7f),
        new DeliveryPerson("Aoi", 0.6f, 0.6f, 2, 0.8f, 0.7f, 0.8f),
        new DeliveryPerson("Ren", 0.5f, 0.3f, 2, 0.5f, 0.9f, 0.4f),
        new DeliveryPerson("Sora", 0.9f, 0.4f, 1, 0.3f, 0.4f, 0.3f)
    };
    private readonly List<Button> buttons = new();
    public DeliveryPerson SelectedPerson { get; private set; }
    public IReadOnlyList<DeliveryPerson> People => deliveryPeople;
    public event Action SelectionChanged;

    private void Start()
    {
        buttons.AddRange(deliveryPersonListContent.GetComponentsInChildren<Button>(true));
        while (buttons.Count < deliveryPeople.Count)
        {
            var button = Instantiate(deliveryPersonItemPrefab, deliveryPersonListContent);
            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 52;
            buttons.Add(button);
        }
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].gameObject.SetActive(i < deliveryPeople.Count);
            if (i >= deliveryPeople.Count) continue;
            DeliveryPerson person = deliveryPeople[i];
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => Select(person));
            buttons[i].GetComponentInChildren<TextMeshProUGUI>().raycastTarget = false;
        }
        Refresh();
    }
    private void Update() => Refresh();
    private void Refresh()
    {
        for (int i = 0; i < deliveryPeople.Count && i < buttons.Count; i++)
        {
            var person = deliveryPeople[i];
            float remaining = Mathf.Max(0, (person.CurrentTrip?.ReturnsAt ?? 0) - clock.ElapsedSeconds);
            string state = person.IsAvailable ? "Idle" : $"{person.Status} / {remaining:F0}s";
            var label = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{person.Name} / {state}\n<size=14>Drive {person.Driving:F1}  Know {person.LocalKnowledge:F1}  Load {person.Capacity}"
                + $"\nService {person.Service:F1}  Endurance {person.Endurance:F1}  Safety {person.Safety:F1}</size>";
            label.fontSize = 17;
            label.color = new Color(0.1f, 0.14f, 0.2f);
            buttons[i].interactable = person.IsAvailable && clock.Phase == DayPhase.Open;
            buttons[i].GetComponent<Image>().color = SelectedPerson == person ? Color.cyan
                : person.IsAvailable ? Color.white : new Color(0.55f, 0.61f, 0.7f);
        }
    }
    private void Select(DeliveryPerson person)
    {
        if (!person.IsAvailable || clock.Phase != DayPhase.Open) return;
        SelectedPerson = SelectedPerson == person ? null : person;
        Refresh();
        SelectionChanged?.Invoke();
    }
    public void ClearSelection()
    {
        SelectedPerson = null;
        Refresh();
        SelectionChanged?.Invoke();
    }
}
