using UnityEngine;
using UnityEngine.UI;
public sealed class TrainingPickerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup choices;
    [SerializeField] private Button toggle;
    private void Start(){UIVisibility.Set(choices.gameObject,false);toggle.onClick.AddListener(()=>UIVisibility.Set(choices.gameObject,choices.alpha<.5f));}
}
