using UnityEngine;
using UnityEngine.UI;
public sealed class TrainingPickerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup choices;
    [SerializeField] private Button toggle;
    private void Start(){UIVisibility.Set(choices.gameObject,false);toggle.onClick.AddListener(()=>UIVisibility.Set(choices.gameObject,choices.alpha<.5f));}
    // Applicant rows deactivate their training choices; hide the toggle there instead of offering an empty popup.
    private void LateUpdate(){bool trainable=choices.gameObject.activeSelf;var group=toggle.GetComponent<CanvasGroup>();if(group==null || group.alpha>.5f!=trainable)UIVisibility.Set(toggle.gameObject,trainable);}
}
