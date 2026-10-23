using TMPro;
using UnityEngine;

/// <summary>The training choice for one courier (six abilities with the result and the fee). Part of the hub prefab's
/// dialog layer; HubView fills it for the chosen person.</summary>
public sealed class TrainingDialogView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title,note;
    [SerializeField] private StickerButton[] abilities=new StickerButton[6];
    [SerializeField] private StickerButton close;
    public StickerButton Close=>close;
    public StickerButton Ability(int index)=>abilities[index];
    /// <summary>Title for the person, and what training costs them (couriers miss deliveries, kitchen staff the line).</summary>
    public void Show(string name,string what)
    {
        title.text=$"{name} の訓練";
        if(note!=null)note.text=what;
    }
}
