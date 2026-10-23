using TMPro;
using UnityEngine;

/// <summary>The training choice for one courier (six abilities with the result and the fee). Part of the hub prefab's
/// dialog layer; HubView fills it for the chosen person.</summary>
public sealed class TrainingDialogView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private StickerButton[] abilities=new StickerButton[6];
    [SerializeField] private StickerButton close;
    public StickerButton Close=>close;
    public StickerButton Ability(int index)=>abilities[index];
    public void Show(string name)=>title.text=$"{name} の訓練";
}
