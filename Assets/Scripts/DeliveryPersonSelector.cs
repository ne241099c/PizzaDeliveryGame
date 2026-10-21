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
    [SerializeField, Min(52)] private float rowHeight = 88f;
    private readonly Dictionary<Button,RectTransform> staminaBars = new();
    private readonly List<Button> buttons = new();
    public DeliveryPerson SelectedPerson { get; private set; }
    public IReadOnlyList<DeliveryPerson> People => deliveryPeople;
    public event Action SelectionChanged;

    private void Start() => RebuildRoster();
    public void ResetDailyState()
    {
        foreach(var p in deliveryPeople) { p.Status=DeliveryStatus.Idle; p.CurrentTrip=null; p.LastReturnedAt=0; }
        ClearSelection();
    }
    public void RebuildRoster()
    {
        if(buttons.Count==0)buttons.AddRange(deliveryPersonListContent.GetComponentsInChildren<Button>(true));
        while (buttons.Count < deliveryPeople.Count)
        {
            var button = Instantiate(deliveryPersonItemPrefab, deliveryPersonListContent);
            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = rowHeight;
            buttons.Add(button);
        }
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].gameObject.SetActive(i < deliveryPeople.Count);
            if (i >= deliveryPeople.Count) continue;
            var rowLayout=buttons[i].GetComponent<LayoutElement>()??buttons[i].gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight=rowLayout.preferredHeight=rowHeight;
            var labelRect=buttons[i].GetComponentInChildren<TextMeshProUGUI>().rectTransform;
            labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;
            labelRect.offsetMin=new Vector2(8,8);labelRect.offsetMax=new Vector2(-8,-4);
            EnsureStaminaBar(buttons[i]);
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
            if(staminaBars.TryGetValue(buttons[i],out var bar))bar.anchorMax=new Vector2(person.CurrentStamina,1);
            buttons[i].interactable = person.IsAvailable && clock.Phase == DayPhase.Open;
            buttons[i].GetComponent<Image>().color = SelectedPerson == person ? Color.cyan
                : person.IsAvailable ? Color.white : new Color(0.55f, 0.61f, 0.7f);
        }
    }
    private void EnsureStaminaBar(Button button)
    {
        Transform existing=button.transform.Find("StaminaBar");
        if(existing==null)
        {
            var go=new GameObject("StaminaBar",typeof(RectTransform),typeof(Image));go.transform.SetParent(button.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0.05f,0);rect.anchorMax=new Vector2(0.95f,0);
            rect.pivot=new Vector2(0.5f,0);rect.anchoredPosition=new Vector2(0,1);rect.sizeDelta=new Vector2(0,4);
            go.GetComponent<Image>().color=new Color(0.2f,0.25f,0.3f);go.GetComponent<Image>().raycastTarget=false;
            var fill=new GameObject("Fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(go.transform,false);
            var fr=fill.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;
            fill.GetComponent<Image>().color=new Color(0.2f,0.75f,0.45f);fill.GetComponent<Image>().raycastTarget=false;
            existing=go.transform;
        }
        staminaBars[button]=existing.Find("Fill").GetComponent<RectTransform>();
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
